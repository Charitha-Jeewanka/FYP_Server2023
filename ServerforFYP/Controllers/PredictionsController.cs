using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Scripting.Hosting;
using Microsoft.Extensions.Logging;
using IronPython.Hosting;
using IronPython.Runtime;

namespace ServerforFYP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PredictionsController : ControllerBase
    {
        private readonly ILogger<PredictionsController> _logger;

        public PredictionsController(ILogger<PredictionsController> logger) 
        { 
            _logger = logger;
        }

        [HttpPost]
        public IActionResult PredictStress()
        {
            IFormFile file = Request.Form.Files[0]; // Get the uploaded file

            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream); // Copy the file content to memory stream

                // Convert the memory stream content to a byte array
                byte[] csvBytes = memoryStream.ToArray();

                // Save the byte array as a temporary CSV file
                string tempFilePath = Path.GetTempFileName();
                System.IO.File.WriteAllBytes(tempFilePath, csvBytes);

                // Call the Python function with the file path
                var engine = Python.CreateEngine();
                var scope = engine.CreateScope();

                // Load the Python script
                var scriptPath = "D:\\FYP\\Codes\\EEG_ML\\PSD_windowing\\predict_stress.py";
                var scriptCode = System.IO.File.ReadAllText(scriptPath);
                engine.Execute(scriptCode, scope);

                // Get the predict_stress function from the Python scope
                var predictStressFunction = scope.GetVariable<Func<string, double[]>>("predict_stress");

                // Call the predict_stress function with the file path
                var predictedStressedPercentage = predictStressFunction(tempFilePath);

                // Delete the temporary file
                System.IO.File.Delete(tempFilePath);

                return Ok(new { PredictedStressedPercentage = predictedStressedPercentage });
            }
        }

    }
}

