using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;

namespace ServerforFYP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly StorageClient _storageClient;
        public FileUploadController(StorageClient storageClient) 
        {
            _storageClient = storageClient;
        }

        [HttpPost(Name = "csvUpload")]
        public async Task<IActionResult> UploadCsvFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentNullException(nameof(file));
            }

            using (var stream = file.OpenReadStream())
            {
                var objectName = "biodata/" + file.FileName;
                var bucketName = "uploads_for_fyp";
                await _storageClient.UploadObjectAsync(bucketName, objectName, null, stream);
            }

            return Ok("File uploaded successfully.");
        }
    }
}
