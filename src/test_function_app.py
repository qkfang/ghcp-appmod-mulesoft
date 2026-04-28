"""
Local unit tests for the BookMyShow Azure Function App.

These tests mock the MySQL database and verify the HTTP handler logic
for both GET /api/movies and POST /api/movies/{m_id} endpoints.
"""
import importlib
import json
import os
import sys
import types
import unittest
from unittest.mock import MagicMock, patch

# ---------------------------------------------------------------------------
# Provide minimal environment variables so function_app.py can be imported
# ---------------------------------------------------------------------------
os.environ.setdefault("DB_HOST", "localhost")
os.environ.setdefault("DB_PORT", "3306")
os.environ.setdefault("DB_USER", "test")
os.environ.setdefault("DB_PASSWORD", "test")
os.environ.setdefault("DB_NAME", "bookmyshow")

# Add src/ to the module search path
SRC_DIR = os.path.join(os.path.dirname(__file__), "..", "src")
sys.path.insert(0, os.path.abspath(SRC_DIR))

import azure.functions as func  # noqa: E402

# Import the functions under test
import function_app  # noqa: E402


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_request(method: str = "GET", route_params: dict | None = None, params: dict | None = None) -> func.HttpRequest:
    return func.HttpRequest(
        method=method,
        url="http://localhost/api/movies",
        route_params=route_params or {},
        params=params or {},
        headers={},
        body=b"",
    )


SAMPLE_MOVIES = [
    {"m_id": 1, "m_name": "Avengers: Endgame", "m_available": 50},
    {"m_id": 2, "m_name": "The Lion King", "m_available": 30},
]

SAMPLE_ORDER = {"o_id": 1, "m_id": 1, "no_tickets": 3, "price": 300}


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

class TestGetMovies(unittest.TestCase):
    @patch("function_app.get_db_connection")
    def test_returns_movie_list(self, mock_conn_factory):
        conn = MagicMock()
        cursor = MagicMock()
        cursor.fetchall.return_value = SAMPLE_MOVIES
        conn.cursor.return_value.__enter__ = MagicMock(return_value=cursor)
        conn.cursor.return_value.__exit__ = MagicMock(return_value=False)
        mock_conn_factory.return_value = conn

        response = function_app.get_movies(_make_request())

        self.assertEqual(response.status_code, 200)
        body = json.loads(response.get_body())
        self.assertEqual(len(body), 2)
        self.assertEqual(body[0]["m_name"], "Avengers: Endgame")

    @patch("function_app.get_db_connection")
    def test_db_error_returns_500(self, mock_conn_factory):
        mock_conn_factory.side_effect = Exception("connection refused")

        response = function_app.get_movies(_make_request())

        self.assertEqual(response.status_code, 500)
        body = json.loads(response.get_body())
        self.assertIn("error", body)


class TestBookTickets(unittest.TestCase):
    def _post(self, m_id: str, no_tickets: str) -> func.HttpResponse:
        req = _make_request(
            method="POST",
            route_params={"m_id": m_id},
            params={"no_tickets": no_tickets},
        )
        return function_app.book_tickets(req)

    @patch("function_app.get_db_connection")
    def test_successful_booking(self, mock_conn_factory):
        conn = MagicMock()
        cursor = MagicMock()
        # SELECT FOR UPDATE: availability check
        # SELECT by exact ID: fetch created order
        cursor.fetchone.side_effect = [{"m_available": 50}, SAMPLE_ORDER]
        cursor.lastrowid = 1
        conn.cursor.return_value.__enter__ = MagicMock(return_value=cursor)
        conn.cursor.return_value.__exit__ = MagicMock(return_value=False)
        mock_conn_factory.return_value = conn

        response = self._post("1", "3")

        self.assertEqual(response.status_code, 200)
        body = json.loads(response.get_body())
        self.assertEqual(body["o_id"], 1)
        self.assertEqual(body["price"], 300)

    def test_missing_params_returns_400(self):
        req = _make_request(method="POST", route_params={"m_id": "1"}, params={})
        response = function_app.book_tickets(req)
        self.assertEqual(response.status_code, 400)

    def test_invalid_m_id_returns_400(self):
        response = self._post("abc", "3")
        self.assertEqual(response.status_code, 400)

    def test_zero_tickets_returns_400(self):
        response = self._post("1", "0")
        self.assertEqual(response.status_code, 400)

    @patch("function_app.get_db_connection")
    def test_insufficient_tickets_returns_400(self, mock_conn_factory):
        conn = MagicMock()
        cursor = MagicMock()
        cursor.fetchone.return_value = {"m_available": 2}
        conn.cursor.return_value.__enter__ = MagicMock(return_value=cursor)
        conn.cursor.return_value.__exit__ = MagicMock(return_value=False)
        mock_conn_factory.return_value = conn

        response = self._post("1", "10")

        self.assertEqual(response.status_code, 400)
        body = json.loads(response.get_body())
        self.assertIn("available tickets is only 2", body["error"])

    @patch("function_app.get_db_connection")
    def test_movie_not_found_returns_404(self, mock_conn_factory):
        conn = MagicMock()
        cursor = MagicMock()
        cursor.fetchone.return_value = None
        conn.cursor.return_value.__enter__ = MagicMock(return_value=cursor)
        conn.cursor.return_value.__exit__ = MagicMock(return_value=False)
        mock_conn_factory.return_value = conn

        response = self._post("999", "1")

        self.assertEqual(response.status_code, 404)
    @patch("function_app.get_db_connection")
    def test_pricing_tier_1_to_5(self, mock_conn_factory):
        """1-5 tickets → price = tickets * 100."""
        conn = MagicMock()
        cursor = MagicMock()
        cursor.fetchone.side_effect = [{"m_available": 50}, {"o_id": 1, "m_id": 1, "no_tickets": 5, "price": 500}]
        cursor.lastrowid = 1
        conn.cursor.return_value.__enter__ = MagicMock(return_value=cursor)
        conn.cursor.return_value.__exit__ = MagicMock(return_value=False)
        mock_conn_factory.return_value = conn

        response = self._post("1", "5")
        self.assertEqual(response.status_code, 200)

        # Verify INSERT was called with correct price (5 * 100 = 500)
        insert_call = cursor.execute.call_args_list[1]
        self.assertEqual(insert_call[0][1][2], 500)

    @patch("function_app.get_db_connection")
    def test_pricing_tier_6_to_10(self, mock_conn_factory):
        """6-10 tickets → price = tickets * 90."""
        conn = MagicMock()
        cursor = MagicMock()
        cursor.fetchone.side_effect = [{"m_available": 50}, {"o_id": 2, "m_id": 1, "no_tickets": 10, "price": 900}]
        cursor.lastrowid = 2
        conn.cursor.return_value.__enter__ = MagicMock(return_value=cursor)
        conn.cursor.return_value.__exit__ = MagicMock(return_value=False)
        mock_conn_factory.return_value = conn

        response = self._post("1", "10")
        self.assertEqual(response.status_code, 200)

        insert_call = cursor.execute.call_args_list[1]
        self.assertEqual(insert_call[0][1][2], 900)

    @patch("function_app.get_db_connection")
    def test_pricing_tier_over_10(self, mock_conn_factory):
        """11+ tickets → price = tickets * 80."""
        conn = MagicMock()
        cursor = MagicMock()
        cursor.fetchone.side_effect = [{"m_available": 50}, {"o_id": 3, "m_id": 1, "no_tickets": 15, "price": 1200}]
        cursor.lastrowid = 3
        conn.cursor.return_value.__enter__ = MagicMock(return_value=cursor)
        conn.cursor.return_value.__exit__ = MagicMock(return_value=False)
        mock_conn_factory.return_value = conn

        response = self._post("1", "15")
        self.assertEqual(response.status_code, 200)

        insert_call = cursor.execute.call_args_list[1]
        self.assertEqual(insert_call[0][1][2], 1200)


if __name__ == "__main__":
    unittest.main()
