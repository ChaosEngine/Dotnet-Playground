using System.Linq;
using DotnetPlayground.Pages;
using DotnetPlayground.Tests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace RazorPages
{
    public class ImportCsvTests : BaseControllerTest
    {
        [Fact]
        public void OnPost_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var model = new ImportCsvModel();
            model.ModelState.AddModelError("CsvData", "Required");

            // Act
            var result = model.OnPost();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<SerializableError>(badRequestResult.Value);
            Assert.True(model.ModelState.IsValid == false);
            Assert.Equal(model.ModelState.Keys.First(), ((SerializableError)badRequestResult.Value).FirstOrDefault().Key);
            
            var serializableError = Assert.IsType<SerializableError>(badRequestResult.Value);
            var firstErrorKey = Assert.IsType<string>(serializableError.FirstOrDefault().Key);
            var firstErrorValueArray = Assert.IsType<string[]>(((SerializableError)badRequestResult.Value).FirstOrDefault().Value);
            var firstErrorValue = Assert.IsType<string>(firstErrorValueArray.FirstOrDefault());

            Assert.Equal(model.ModelState.Keys.First(), firstErrorKey);
            Assert.Equal(model.ModelState.Values.First().Errors.First().ErrorMessage, firstErrorValue);

        }

        [Fact]
        public void OnPost_NullOrEmptyCsvData_ReturnsBadRequest()
        {
            // Arrange
            var model = new ImportCsvModel
            {
                CsvData = null
            };

            // Act
            var result = model.OnPost();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Error: empty or invalid CSV data.", badRequestResult.Value);
        }

        [Fact]
        public void OnPost_ValidCsvData_ReturnsJsonResult()
        {
            // Arrange
            var csvData = new[]
            {
                new[] { "A", "B", "C" },
                new[] { "1", "", "3" },
                new[] { "", "", "" }
            };

            var model = new ImportCsvModel
            {
                CsvData = csvData
            };

            // Act
            var result = model.OnPost();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var csvResult = Assert.IsType<ImportCsvModel.CsvResult>(jsonResult.Value);

            Assert.Equal(3, csvResult.RowsCount);
            Assert.Equal(5, csvResult.NonEmptyCellsCount);
        }
    }
}
