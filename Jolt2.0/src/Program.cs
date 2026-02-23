using MudBlazor.Services;
using Jolt2._0.Components;
using TwitchLib.EventSub.Websockets.Extensions;
using Nixill.Streaming.JoltBot.Scheduled;
using Nixill.Streaming.JoltBot.Twitch.EventSub;

namespace Nixill.Streaming.JoltBot;

public static class JoltMain
{
  /// <summary>The main program method.</summary>
  public static void Main(string[] args)
  {
    // Set current directory for output and stuff
    var directory = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      "JoltStreamBot"
    );

    Directory.CreateDirectory(directory);
    Directory.SetCurrentDirectory(directory);

    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Add MudBlazor services
    builder.Services.AddMudServices();

    // Add other services
    builder.Services.AddTwitchLibEventSubWebsockets()
      .AddHostedService<JoltEventService>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
      app.UseExceptionHandler("/Error", createScopeForErrors: true);
      // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
      app.UseHsts();
    }

    app.UseHttpsRedirection();


    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // Other setup stuff time
    ScheduledActions.RunAll();

    app.Run();
  }
}
