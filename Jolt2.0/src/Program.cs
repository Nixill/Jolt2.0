using MudBlazor.Services;
using Jolt2._0.Components;
using TwitchLib.EventSub.Websockets.Extensions;

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

    // Add MudBlazor services
    builder.Services.AddMudServices();

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.AddTwitchLibEventSubWebsockets();

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

    app.Run();
  }
}
