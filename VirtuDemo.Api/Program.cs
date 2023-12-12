using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

# region builder settings
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
#endregion

#region AppSettings
var blobSettings = builder.Configuration.GetSection("BlobSettings");
var connectionString = blobSettings["ConnectionString"];
var containerName = blobSettings["Container"];
#endregion

#region Services Register
builder.Services.AddSingleton(new BlobServiceClient(connectionString));
#endregion

#region App use
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
#endregion

app.MapPost("/api/blobstorage/upload", async ([FromForm] IFormFile formFile, BlobServiceClient blobServiceClient, HttpContext httpContext) =>
{
    if (formFile == null || formFile.Length == 0)
    {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsync("Invalid file");
        return;
    }

    try
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(formFile.FileName);

        using (Stream stream = formFile.OpenReadStream())
        {
            await blobClient.UploadAsync(stream, true);
        }

        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        await httpContext.Response.WriteAsync("File uploaded successfully to Blob Storage");
    }
    catch (Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsync($"Internal server error: {ex.Message}");
    }
})
.DisableAntiforgery();

app.Run();