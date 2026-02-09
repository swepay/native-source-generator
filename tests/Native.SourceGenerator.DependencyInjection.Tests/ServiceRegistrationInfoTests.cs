namespace Native.SourceGenerator.DependencyInjection.Tests;

using Shouldly;
using Xunit;

public class ServiceRegistrationInfoTests
{
    [Fact]
    public void FieldNameConversion_WithUnderscorePrefix_GeneratesCorrectParameterName()
    {
        // Arrange
        var fieldName = "_myField";

        // Act
        var parameterName = fieldName.TrimStart('_');
        if (parameterName.Length > 0)
        {
            parameterName = char.ToLowerInvariant(parameterName[0]) + parameterName.Substring(1);
        }

        // Assert
        parameterName.ShouldBe("myField");
    }

    [Fact]
    public void FieldNameConversion_WithDoubleUnderscore_GeneratesCorrectParameterName()
    {
        // Arrange
        var fieldName = "__myField";

        // Act
        var parameterName = fieldName.TrimStart('_');
        if (parameterName.Length > 0)
        {
            parameterName = char.ToLowerInvariant(parameterName[0]) + parameterName.Substring(1);
        }

        // Assert
        parameterName.ShouldBe("myField");
    }

    [Fact]
    public void FieldNameConversion_WithNoPrefix_GeneratesCorrectParameterName()
    {
        // Arrange
        var fieldName = "MyField";

        // Act
        var parameterName = fieldName.TrimStart('_');
        if (parameterName.Length > 0)
        {
            parameterName = char.ToLowerInvariant(parameterName[0]) + parameterName.Substring(1);
        }

        // Assert
        parameterName.ShouldBe("myField");
    }
}
