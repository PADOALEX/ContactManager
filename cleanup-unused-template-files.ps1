$paths = @(
    "src/ContactManager.Web/Views/Home/Index.cshtml",
    "src/ContactManager.Web/Views/Home/Privacy.cshtml",
    "src/ContactManager.Web/Views/Shared/_Layout.cshtml.css",
    "src/ContactManager.Web/Views/Shared/_ValidationScriptsPartial.cshtml",
    "src/ContactManager.Web/wwwroot/js/site.js",
    "src/ContactManager.Web/wwwroot/lib/jquery",
    "src/ContactManager.Web/wwwroot/lib/jquery-validation",
    "src/ContactManager.Web/wwwroot/lib/jquery-validation-unobtrusive",
    "Samples/test.txt"
)

foreach ($path in $paths) {
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force
        Write-Host "Removed $path"
    }
}
