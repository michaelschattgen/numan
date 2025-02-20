using NuGet.Versioning;
using Numan.Config;
using Numan.Models;
using Numan.Utils;
using Spectre.Console;

namespace Numan.Commands;

public class RemovePackagesCommand : BaseCommand
{
    public void Execute(string? sourceName, bool deleteAllVersions = false)
    {
        PreExecute(sourceName);

        var config = ConfigManager.Config;
        if (config.NugetSources.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No local NuGet sources found.[/]");
            return;
        }

        if (string.IsNullOrWhiteSpace(sourceName))
        {
            if (config.NugetSources.Count > 1)
            {
                var defaultSource = ConfigManager.GetDefaultSource();
                sourceName = defaultSource.Name ?? defaultSource.Value;
                AnsiConsole.MarkupLine($"[yellow]Multiple local NuGet sources found, using default source ({sourceName}). You can override this by using the --source parameter.[/]");
            }
        }

        List<PackageInfo> installedPackages = new();
        foreach (var source in config.NugetSources.Where(x => x.Name == sourceName || x.Value == sourceName))
        {
            installedPackages.AddRange(NuGetUtils.GetInstalledPackages(source.Value, !deleteAllVersions));
        }

        List<PackageInfo> selectedPackages;

        if (deleteAllVersions)
        {
            var selectedPackageNames = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("[blue]Select packages to delete (all versions will be removed):[/]")
                    .PageSize(10)
                    .MoreChoicesText("[gray](Move up and down to reveal more packages)[/]")
                    .InstructionsText("[gray](Press [blue]<space>[/] to select, [green]<enter>[/] to confirm)[/]")
                    .AddChoices(installedPackages.Select(pkg => pkg.Name).Distinct().ToArray())
            );

            selectedPackages = installedPackages
                .Where(pkg => selectedPackageNames.Contains(pkg.Name))
                .ToList();
        }
        else
        {
            var packageChoices = installedPackages
                .SelectMany(pkg => pkg.Versions.Select(version => new { Package = pkg, Version = version }))
                .ToList();

            var selectedPackageChoices = AnsiConsole.Prompt(
                new MultiSelectionPrompt<(PackageInfo, NuGetVersion)>()
                    .Title("[blue]Select package versions to delete:[/]")
                    .PageSize(10)
                    .MoreChoicesText("[gray](Move up and down to reveal more packages)[/]")
                    .InstructionsText("[gray](Press [blue]<space>[/] to select, [green]<enter>[/] to confirm)[/]")
                    .AddChoices(packageChoices.Select(p => (p.Package, p.Version)))
                    .UseConverter(p => $"{p.Item1.Name} {p.Item2}")
            );

            selectedPackages = selectedPackageChoices
                .Select(p => p.Item1)
                .Distinct()
                .ToList();

            foreach (var (package, version) in selectedPackageChoices)
            {
                package.Versions.Remove(version);
            }
        }

        if (selectedPackages.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No packages selected for deletion.[/]");
            return;
        }

        if (!AnsiConsole.Confirm("[red]Are you sure you want to delete the selected packages? This action cannot be undone.[/]"))
        {
            return;
        }

        foreach (var package in selectedPackages)
        {
            foreach (var version in package.Versions)
            {
                string packagePath = package.GetPackageFilePath(version);

                if (File.Exists(packagePath))
                {
                    try
                    {
                        File.Delete(packagePath);
                        AnsiConsole.MarkupLine($"[green]Deleted:[/] {package.Name} {version} from {package.Source}");
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Failed to delete {package.Name} {version}: {ex.Message}[/]");
                    }
                }
            }
        }
    }
}
