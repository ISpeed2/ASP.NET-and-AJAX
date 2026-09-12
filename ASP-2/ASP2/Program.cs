using Microsoft.Extensions.DependencyInjection;
using ASP2;

namespace ASP2;

internal class Program
{
    public class ScopedService : IDisposable
    {
        public ScopedService() => Console.WriteLine("Scoped created");
        public void Dispose() => Console.WriteLine("Scoped disposed");
    }

    public class TransientService { }

    public class SingletonService
    {
        private readonly ScopedService _scoped;
        private readonly TransientService _transient;

        public SingletonService(ScopedService scoped, TransientService transient)
        {
            _scoped = scoped;
            _transient = transient;
        }

        public void DoWork() => Console.WriteLine("Working");
    }

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();
        //builder.Services.AddTransient<ICounter, Counter>();
        //builder.Services.AddScoped<ICounter, Counter>();
        //builder.Services.AddSingleton<ICounter, Counter>();

        builder.Services.AddScoped<ICounter, Counter>();
        builder.Services.AddScoped<ScopedService>();
        builder.Services.AddTransient<TransientService>();
        // Включаем валидацию
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        var app = builder.Build();
        app.MapGet("/", () => "pong");

        app.MapControllerRoute(name: "default", pattern: "{controller = Counter}/{action = Get}");

        app.Run();
    }
}