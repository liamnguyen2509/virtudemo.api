using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;

# region builder settings
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
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

app.UseCors(builder => builder
 .AllowAnyOrigin()
 .AllowAnyMethod()
 .AllowAnyHeader()
);
#endregion

app.MapGet("/api/getsaslink", async (string fileName, BlobServiceClient blobServiceClient, HttpContext httpContext) =>
{
    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    var blobClient = containerClient.GetBlobClient(fileName);

    BlobSasBuilder sasBuilder = new BlobSasBuilder
    {
        BlobContainerName = containerName,
        BlobName = fileName,
        Resource = "b",
        StartsOn = DateTimeOffset.UtcNow,
        ExpiresOn = DateTimeOffset.UtcNow.AddHours(1), // Set expiry time
    };

    sasBuilder.SetPermissions(BlobSasPermissions.Write); // Set the permissions for the SAS

    Uri blobUri = blobClient.GenerateSasUri(sasBuilder);

    httpContext.Response.StatusCode = StatusCodes.Status200OK;
    await httpContext.Response.WriteAsJsonAsync(blobUri.ToString());
});

app.MapPost("/api/blobstorage/upload", async ([FromForm] IFormFile formFile, BlobServiceClient blobServiceClient, HttpContext httpContext) =>
{
    if (formFile == null || formFile.Length == 0)
    {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync("Invalid file");
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
        await httpContext.Response.WriteAsJsonAsync("File uploaded successfully to Blob Storage");
    }
    catch (Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync($"Internal server error: {ex.Message}");
    }
})
.DisableAntiforgery();

app.Run();
