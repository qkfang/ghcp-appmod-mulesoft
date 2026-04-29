namespace BookMyShow.Functions;

public sealed record Movie(int M_Id, string M_Name, int M_Available);

public sealed record Order(int O_Id, int M_Id, int No_Tickets, int Price);

public sealed record ErrorResponse(string Error);
