using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ServerforFYP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PredictionsController : ControllerBase
    {
        private readonly StorageClient _storageClient;

        public PredictionsController(StorageClient storageClient)
        {
            _storageClient = storageClient;
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> PredictStress(IFormFile file, [FromForm] string selectedDataType)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                // Upload the file to Google Cloud Storage
                string bucketName = "uploads_for_fyp";
                string objectName = Guid.NewGuid().ToString();

                using (var stream = file.OpenReadStream())
                {
                    await _storageClient.UploadObjectAsync(bucketName, objectName, null, stream);
                }

                // Subprocess
                string pythonScriptPath = @"D:\FYP\Server\ServerforFYP\ServerforFYP\Assets\predict_stress.py";

                string bucketPath = $"gs://{bucketName}/{objectName}";
                string arguments = $"\"{pythonScriptPath}\" \"{bucketPath}\"";

                ProcessStartInfo processInfo = new ProcessStartInfo("python");

                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.Arguments = arguments;

                using (var process = Process.Start(processInfo))
                {
                    StreamReader streamReader = process.StandardOutput;
                    string output = streamReader.ReadLine();
                    process.WaitForExit();

                    await _storageClient.DeleteObjectAsync(bucketName, objectName);

                    return Ok(output);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}
