using Serilog;
using TravelAgency.Chat.API.Extensions;
using TravelAgency.Chat.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddChatSerilog();

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddChatAuthentication(builder.Configuration);
builder.Services.AddChatAuthorization();
builder.Services.AddChatInfrastructure(builder.Configuration);
builder.Services.AddChatCors(builder.Configuration);
builder.Services.AddChatHealthChecks(builder.Configuration);
builder.Services.AddChatTracing();

var app = builder.Build();

app.UseChatMigrations();
app.UseChatCors();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TravelAgency.Chat.API.Hubs.ChatHub>("/hubs/chat");
app.MapChatHealthChecks();

app.Run();

public partial class Program { }
