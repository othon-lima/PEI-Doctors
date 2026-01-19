using Xunit;

namespace PEI_Doctors.Tests;

public class NormalizeJsonTests
{
    [Fact]
    public void NormalizeJson_WithCompactJson_ReturnsIndentedJson()
    {
        // Arrange
        string compactJson = "{\"name\":\"test\",\"value\":123}";
        
        // Act
        string result = Program.NormalizeJson(compactJson);
        
        // Assert
        // The default normalization in Program.NormalizeJson uses WriteIndented = true
        // which typically puts a space after the colon and indents nested properties.
        Assert.Contains("\"name\": \"test\"", result);
        Assert.Contains("\n", result); // Verify it has newlines (is indented)
    }

    [Fact]
    public void NormalizeJson_ValidJson_ReturnsEquivalentJson()
    {
        // Arrange
        string rawJson = "{\"a\":1}";

        // Act
        string result = Program.NormalizeJson(rawJson);

        // Assert
        Assert.Contains("\"a\": 1", result);
    }
}
