using System;
namespace EFUtility.CodeGenerationTools
{
/// <summary>
/// Responsible for creating source code regions in code when the loop inside
/// actually produces something.
/// </summary>
public class CodeRegion
{
    private const int STANDARD_INDENT_LENGTH = 4;

    private readonly DynamicTextTransformation _textTransformation;
    private int _beforeRegionLength;
    private int _emptyRegionLength;
    private int _regionIndentLevel = -1;

    /// <summary>
    /// Initializes an CodeRegion instance with the
    /// TextTransformation (T4 generated class) that is currently running
    /// </summary>
    public CodeRegion(object textTransformation)
    {
        if (textTransformation == null)
        {
            throw new ArgumentNullException("textTransformation");
        }

        _textTransformation = DynamicTextTransformation.Create(textTransformation);
    }

    /// <summary>
    /// Initializes an CodeRegion instance with the
    /// TextTransformation (T4 generated class) that is currently running,
    /// and the indent level to start the first region at.
    /// </summary>
    public CodeRegion(object textTransformation, int firstIndentLevel)
        : this(textTransformation)
    {
        if (firstIndentLevel < 0)
        {
            throw new ArgumentException("firstIndentLevel");
        }

        _regionIndentLevel = firstIndentLevel - 1;
    }

    /// <summary>
    /// Starts the begining of a region
    /// </summary>
    public void Begin(string regionName)
    {
        if (regionName == null)
        {
            throw new ArgumentNullException("regionName");
        }

        Begin(regionName, 1);
    }

    /// <summary>
    /// Start the begining of a region, indented
    /// the numbers of levels specified
    /// </summary>
    public void Begin(string regionName, int levelsToIncreaseIndent)
    {
        if (regionName == null)
        {
            throw new ArgumentNullException("regionName");
        }

        _beforeRegionLength = _textTransformation.GenerationEnvironment.Length;
        _regionIndentLevel += levelsToIncreaseIndent;
        _textTransformation.Write(GetIndent(_regionIndentLevel));
        _textTransformation.WriteLine("#region " + regionName);
        _emptyRegionLength = _textTransformation.GenerationEnvironment.Length;
    }

    /// <summary>
    /// Ends a region, or totaly removes it if nothing
    /// was generted since the begining of the region.
    /// </summary>
    public void End()
    {
        End(1);
    }

    /// <summary>
    /// Ends a region, or totaly removes it if nothing
    /// was generted since the begining of the region, also outdents
    /// the number of levels specified.
    /// </summary>
    public void End(int levelsToDecrease)
    {
        int indentLevel = _regionIndentLevel;
        _regionIndentLevel -= levelsToDecrease;

        if (_emptyRegionLength == _textTransformation.GenerationEnvironment.Length)
            _textTransformation.GenerationEnvironment.Length = _beforeRegionLength;
        else
        {
            _textTransformation.WriteLine(String.Empty);
            _textTransformation.Write(GetIndent(indentLevel));
            _textTransformation.WriteLine("#endregion");
            _textTransformation.WriteLine(String.Empty);
        }
    }

    /// <summary>
    /// Gets the current indent level that the next end region statement will be written
    /// at
    /// </summary>
    public int CurrentIndentLevel { get { return _regionIndentLevel; } }

    /// <summary>
    /// Get a string of spaces equivelent to the number of indents
    /// desired.
    /// </summary>
    public static string GetIndent(int indentLevel)
    {
        if (indentLevel < 0)
        {
            throw new ArgumentException("indentLevel");
        }

        return String.Empty.PadLeft(indentLevel * STANDARD_INDENT_LENGTH);
    }
}
}
