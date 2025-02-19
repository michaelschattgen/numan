using Numan.Config;
using Numan.Models;
using Numan.Utils;
using Spectre.Console;

namespace Numan.Commands;

public class ListPackagesCommand : BaseCommand
{
    public void Execute(string sourceName)
    {
        PreExecute();

        var config = ConfigManager.Config;
        NugetSource? source;

        if (string.IsNullOrWhiteSpace(sourceName))
        {
            if (config.NugetSources.Count > 1)
            {
                AnsiConsole.MarkupLine($"[red]Please specify a source.[/]");
                return;
            }

            source = config.NugetSources.FirstOrDefault();
        }
        else
        {
            source = config.NugetSources.Find(s => s.Name != null && s.Name.Equals(sourceName, StringComparison.OrdinalIgnoreCase));
            if (source == null)
            {
                AnsiConsole.MarkupLine($"[red]Error: NuGet source '{sourceName}' not found.[/]");
                return;
            }
        }

        if (source == null)
        {
            AnsiConsole.MarkupLine($"[red]No sources found.[/]");
            return;
        }

        var latestPackages = NuGetUtils.GetInstalledPackages(source.Value);
        if (latestPackages.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No installed packages found.[/]");
            return;
        }

        var table = new Table();
        table.Border = TableBorder.Rounded;
        table.AddColumn("[cyan]Package[/]");
        table.AddColumn("[green]Latest Version[/]");

        foreach (var package in latestPackages)
        {
            table.AddRow($"[cyan]{package.Key}[/]", $"[green]{package.Value.First()}[/]");
        }

        AnsiConsole.Write(table);
    }
}
