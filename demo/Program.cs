using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>{builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();});
});

var app = builder.Build();
app.UseCors("AllowAllOrigins");

List<Order> repo = new List<Order>()
{
    new Order(1,"компьютер","asus","зависает","Штайгер Вадим Андреевич", "+79931092706","ожидание запчастей","не назначен",11,10,2024),
    new Order(2,"принтер","hp","не печатает","Иванов Иван Иванович", "+79998887766","в процессе ремонта","Сырник",08,10,2024),
    new Order(3,"факс","hp","не звонит","Пупкин Василий Васильевич", "+79998887722","готова к выдаче","Плюшечка",05,10,2024)
};

bool isUpdatedStatus = false;
string message = "";

app.MapGet("/", () => {
    if (isUpdatedStatus)
    {
        string buffer = message;
        isUpdatedStatus = false;
        message = "";
        return Results.Json(new OrderUpdateStatusDTO(repo, buffer));
    }
    else
        return Results.Json(repo);
});

app.MapGet("/{num}", (int num) => repo.Find(o => o.Number == num));

app.MapPost("/", (Order order) => repo.Add(order));

app.MapPut("/{num}", ( int num ,OrderUpdateDTO dto) =>
{
    Order buffer = repo.Find(o => o.Number == num);
    if (buffer == null)
        return Results.StatusCode(404);
    if (buffer.Description != dto.Description)
        buffer.Description = dto.Description;
    if (buffer.Master != dto.Master)
        buffer.Master = dto.Master;    
    if(buffer.Status != dto.Status)
    {
    buffer.Status = dto.Status;
    isUpdatedStatus = true;
    message += "Статус заявки номер " + buffer.Number + " изменен\n";
    if(buffer.Status == "завершено")
        buffer.EndDate = DateTime.Now;
    }
    if(dto.Comment != null || dto.Comment != "")
    buffer.Comments.Add(dto.Comment);
    return Results.Json(buffer);
});

app.MapGet("/stat/complCount", () => repo.FindAll(o => o.Status == "готова к выдаче"));

app.MapGet("/stat/Description", () =>{
    Dictionary<string,int> result =[];
    foreach (var o in repo)
        if(result.ContainsKey(o.Description))
            result[o.Description]++;
        else
            result[o.Description] = 1;
        return result;
});

app.MapGet("/stat/avrg",() =>{
    double Sum = 0;
    int Count = 0;
    foreach(var o in repo)
        if(o.Status == "готова к выдаче")
        {
        Sum += o.TimeInDay();
        Count++;
        }
    return Count > 0 ? Sum / Count : 0;
});

app.Run();

record class OrderUpdateDTO(string Status, string Description, string Master,string Comment);
record class OrderUpdateStatusDTO(List<Order> repo, string message);
class Order
{
    int number;
    string officeEquipment;
    string model;
    string description;
    string fio;
    string phone;
    string status;
    string master;

    public Order(int number, string officeEquipment, string model, string description, string fio, string phone, string status,string master,int day,int month,int year)
    {
        Number = number;
        OfficeEquipment = officeEquipment;
        Model = model;
        Description = description;
        Fio = fio;
        Phone = phone;
        Status = status;
        Master = master;
        StartDate = new DateTime(year,month,day);
        EndDate = null;
    }

    public int Number { get => number; set => number = value; }
    public string OfficeEquipment { get => officeEquipment; set => officeEquipment = value; }
    public string Model { get => model; set => model = value; }
    public string Description { get => description; set => description = value; }
    public string Fio { get => fio; set => fio = value; }
    public string Phone { get => phone; set => phone = value; }
    public string Status { get => status; set => status = value; }
    public string Master { get => master; set => master = value; }
    public DateTime StartDate {get;set;}
    public DateTime? EndDate {get;set;}
    public double TimeInDay() => (EndDate - StartDate).Value.TotalDays;
    public List<string> Comments {get;set;} = [];
}
