import azure.functions as func
import json
import logging
import os

import pymysql
import pymysql.cursors

app = func.FunctionApp(http_auth_level=func.AuthLevel.ANONYMOUS)


def get_db_connection():
    """Create and return a MySQL database connection."""
    return pymysql.connect(
        host=os.environ["DB_HOST"],
        port=int(os.environ.get("DB_PORT", "3306")),
        user=os.environ["DB_USER"],
        password=os.environ["DB_PASSWORD"],
        database=os.environ["DB_NAME"],
        cursorclass=pymysql.cursors.DictCursor,
        autocommit=False,
    )


@app.route(route="movies", methods=["GET"])
def get_movies(req: func.HttpRequest) -> func.HttpResponse:
    """GET /api/movies - Return all movies with available tickets."""
    logging.info("GetMovies function triggered.")
    conn = None
    try:
        conn = get_db_connection()
        with conn.cursor() as cursor:
            cursor.execute("SELECT * FROM movie_table WHERE m_available > 0")
            movies = cursor.fetchall()
        return func.HttpResponse(
            body=json.dumps(movies),
            mimetype="application/json",
            status_code=200,
        )
    except Exception as exc:
        logging.error("Error in get_movies: %s", exc)
        return func.HttpResponse(
            body=json.dumps({"error": str(exc)}),
            mimetype="application/json",
            status_code=500,
        )
    finally:
        if conn:
            conn.close()


@app.route(route="movies/{m_id}", methods=["POST"])
def book_tickets(req: func.HttpRequest) -> func.HttpResponse:
    """POST /api/movies/{m_id}?no_tickets=N - Book tickets for a movie."""
    logging.info("BookTickets function triggered.")

    m_id_str = req.route_params.get("m_id")
    no_tickets_str = req.params.get("no_tickets")

    if not m_id_str or not no_tickets_str:
        return func.HttpResponse(
            body=json.dumps({"error": "m_id (path) and no_tickets (query param) are required"}),
            mimetype="application/json",
            status_code=400,
        )

    try:
        m_id = int(m_id_str)
        no_tickets = int(no_tickets_str)
    except ValueError:
        return func.HttpResponse(
            body=json.dumps({"error": "m_id and no_tickets must be integers"}),
            mimetype="application/json",
            status_code=400,
        )

    if no_tickets <= 0:
        return func.HttpResponse(
            body=json.dumps({"error": "no_tickets must be greater than 0"}),
            mimetype="application/json",
            status_code=400,
        )

    conn = None
    try:
        conn = get_db_connection()
        with conn.cursor() as cursor:
            # Lock the row to prevent concurrent overbooking
            cursor.execute(
                "SELECT m_available FROM movie_table WHERE m_id = %s FOR UPDATE",
                (m_id,),
            )
            movie = cursor.fetchone()

            if not movie:
                return func.HttpResponse(
                    body=json.dumps({"error": "Movie not found"}),
                    mimetype="application/json",
                    status_code=404,
                )

            available = int(movie["m_available"])
            if available - no_tickets < 0:
                return func.HttpResponse(
                    body=json.dumps(
                        {
                            "error": (
                                f"available tickets is only {available} "
                                f"but you have ordered {no_tickets}"
                            )
                        }
                    ),
                    mimetype="application/json",
                    status_code=400,
                )

            # Pricing tiers (mirrors original MuleSoft logic)
            if no_tickets <= 5:
                price = no_tickets * 100
            elif no_tickets <= 10:
                price = no_tickets * 90
            else:
                price = no_tickets * 80

            # Insert order
            cursor.execute(
                "INSERT INTO order_table (m_id, no_tickets, price) VALUES (%s, %s, %s)",
                (m_id, no_tickets, price),
            )
            new_order_id = cursor.lastrowid

            # Decrement available seats
            cursor.execute(
                "UPDATE movie_table SET m_available = m_available - %s WHERE m_id = %s",
                (no_tickets, m_id),
            )

            # Return the newly created order using the exact ID from INSERT
            cursor.execute(
                "SELECT * FROM order_table WHERE o_id = %s", (new_order_id,)
            )
            order = cursor.fetchone()

        conn.commit()
        return func.HttpResponse(
            body=json.dumps(order),
            mimetype="application/json",
            status_code=200,
        )
    except Exception as exc:
        if conn:
            conn.rollback()
        logging.error("Error in book_tickets: %s", exc)
        return func.HttpResponse(
            body=json.dumps({"error": str(exc)}),
            mimetype="application/json",
            status_code=500,
        )
    finally:
        if conn:
            conn.close()
