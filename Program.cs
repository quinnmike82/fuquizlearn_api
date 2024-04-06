using System.ComponentModel;
using System.Text.Json.Serialization;
using fuquizlearn_api.Authorization;
using fuquizlearn_api.GameSocket;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Middleware;
using fuquizlearn_api.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNet.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SendGrid.Extensions.DependencyInjection;
using Stripe;

var builder = WebApplication.CreateBuilder(args);



// add services to DI container
{
    var services = builder.Services;
    var appSettings = Config(builder.Services, builder.Configuration);
    var env = builder.Environment;

    StripeConfiguration.ApiKey = builder.Configuration.GetValue<string>("AppSettings:StripeKey:SecretKey");

    services.AddDbContext<DataContext>();
    services.AddCors();
    services.AddControllers().AddJsonOptions(x =>
    {
        // serialize enums as strings in api responses (e.g. Role)
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
    services.AddAutoMapper(cfg => cfg.AllowNullCollections = true, AppDomain.CurrentDomain.GetAssemblies());
    services.AddSwaggerGen(option =>
    {
        option.SwaggerDoc("v1", new OpenApiInfo { Title = "FuQuizLearn API", Version = "v1" });
        option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter a valid token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        });
        option.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] { }
            }
        });
        option.EnableAnnotations();
    });
    
    AppSettings Config(IServiceCollection services, IConfiguration configuration)
    {
        var appSettings = new AppSettings();
        configuration.GetSection("AppSettings").Bind(appSettings);
        services.AddSingleton(appSettings);
        return appSettings;
    }

    // configure strongly typed settings object
    services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
    services.AddHangfire(config =>
        config.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(
            builder.Configuration.GetConnectionString("Supabase")
        )));


    var sendGridApiKey = builder.Configuration.GetValue<string>("AppSettings:SendGridApiKey");
    if (sendGridApiKey == null) throw new AppException("No SendGrid api key provided !");

    var geminiAIApiKey = builder.Configuration.GetValue<string>("AppSettings:GeminiAIApiKey");
    if (geminiAIApiKey == null) throw new AppException(("No GeminiAI api key provided!"));

    // configure DI for application services
    services.AddScoped<IJwtUtils, JwtUtils>();
    services.AddScoped<IHelperEncryptService, HelperEncryptService>();
    services.AddScoped<IHelperDateService, HelperDateService>();
    services.AddScoped<IHelperCryptoService, HelperCryptoService>();
    services.AddScoped<IHelperFrontEnd, HelperFrontEnd>();
    services.AddScoped<IGoogleService, GoogleService>();
    services.AddScoped<IAccountService, fuquizlearn_api.Services.AccountService>();
    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<IQuizBankService, QuizBankService>();
    services.AddScoped<IQuizService, QuizService>();
    services.AddScoped<IGeminiAIService, GeminiService>();
    services.AddScoped<IClassroomService, ClassroomService>();
    services.AddScoped<IPostService, PostService>();
    services.AddScoped<ISearchTextService, SearchTextService>();
    services.AddScoped<INotificationService, NotificationService>();
    services.AddScoped<IPlanService, fuquizlearn_api.Services.PlanService>();
    services.AddScoped<IGameService, GameService>();
    services.AddScoped<IReportService, ReportService>();
    services.AddScoped<ITransactionService, TransactionService>();
    services.AddSendGrid(options => { options.ApiKey = sendGridApiKey; });
    services.AddTransient<IEmbeddingQueueService, EmbeddingQueueService>();
    services.AddHttpClient("GeminiAITextOnly", opt =>
    {
        opt.BaseAddress = new Uri($"{appSettings.TextOnlyUrl}");
    });
    services.AddHttpClient("GeminiAITextAndImage", opt =>
    {
        opt.BaseAddress = new Uri($"{appSettings.TextAndImageUrl}");
    });
    
    services.AddHttpClient("Gemini Embedding", opt =>
    {
        opt.BaseAddress = new Uri($"{appSettings.EmbeddingUrl}");
    });
    
    services.AddSignalR(hubOptions =>
    {
        hubOptions.EnableDetailedErrors = true;
    });
}

var app = builder.Build();

// migrate any database changes on startup (includes initial db creation)
using (var scope = app.Services.CreateScope())
{
    var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
    dataContext.Database.Migrate();
}

// configure HTTP request pipeline
{
    // generated swagger json and swagger ui middleware
    var prefix = Environment.GetEnvironmentVariable("ASPNETCORE_PATHBASE") ?? "/api";
    app.UseSwagger();
    app.UseSwaggerUI(x => x.SwaggerEndpoint("/swagger/v1/swagger.json", ".NET Sign-up and Verification API"));

    // global cors policy
    app.UseCors(x => x
        .SetIsOriginAllowed(origin => true)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());

    app.UseHangfireDashboard(("/hangfire"));
    app.UseHangfireServer();
    if (builder.Environment.IsDevelopment()) app.UseMiddleware<RequestLoggingMiddleware>();
    // global error handler
    app.UseMiddleware<ErrorHandlerMiddleware>();

    app.UseMiddleware<JwtMiddleware>();
    app.UsePathBase(prefix);

    app.MapControllers();
    app.MapHub<GameSocket>("/GameSocket");
}

app.Run();