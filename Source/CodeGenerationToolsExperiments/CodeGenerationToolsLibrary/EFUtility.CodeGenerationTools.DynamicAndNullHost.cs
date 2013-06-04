using System;
using System.Reflection;
namespace EFUtility.CodeGenerationTools
{
/// <summary>
/// Reponsible for abstracting the use of Host between times
/// when it is available and not
/// </summary>
public interface IDynamicHost
{
    /// <summary>
    /// An abstracted call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost ResolveParameterValue
    /// </summary>
    string ResolveParameterValue(string id, string name, string otherName);

    /// <summary>
    /// An abstracted call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost ResolvePath
    /// </summary>
    string ResolvePath(string path);

    /// <summary>
    /// An abstracted call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost TemplateFile
    /// </summary>
    string TemplateFile { get; }

    /// <summary>
    /// Returns the Host instance cast as an IServiceProvider
    /// </summary>
    IServiceProvider AsIServiceProvider();
}

/// <summary>
/// Reponsible for implementing the IDynamicHost as a dynamic
/// shape wrapper over the Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost interface
/// rather than type dependent wrapper.  We don't use the
/// interface type so that the code can be run in preprocessed mode
/// on a .net framework only installed machine.
/// </summary>
public class DynamicHost : IDynamicHost
{
    private readonly object _instance;
    private readonly MethodInfo _resolveParameterValue;
    private readonly MethodInfo _resolvePath;
    private readonly PropertyInfo _templateFile;

    /// <summary>
    /// Creates an instance of the DynamicHost class around the passed in
    /// Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost shapped instance passed in.
    /// </summary>
    public DynamicHost(object instance)
    {
        _instance = instance;
        Type type = _instance.GetType();
        _resolveParameterValue = type.GetMethod("ResolveParameterValue", new Type[] { typeof(string), typeof(string), typeof(string) });
        _resolvePath = type.GetMethod("ResolvePath", new Type[] { typeof(string) });
        _templateFile = type.GetProperty("TemplateFile");

    }

    /// <summary>
    /// A call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost ResolveParameterValue
    /// </summary>
    public string ResolveParameterValue(string id, string name, string otherName)
    {
        return (string)_resolveParameterValue.Invoke(_instance, new object[] { id, name, otherName });
    }

    /// <summary>
    /// A call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost ResolvePath
    /// </summary>
    public string ResolvePath(string path)
    {
        return (string)_resolvePath.Invoke(_instance, new object[] { path });
    }

    /// <summary>
    /// A call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost TemplateFile
    /// </summary>
    public string TemplateFile
    {
        get
        {
            return (string)_templateFile.GetValue(_instance, null);
        }
    }

    /// <summary>
    /// Returns the Host instance cast as an IServiceProvider
    /// </summary>
    public IServiceProvider AsIServiceProvider()
    {
        return _instance as IServiceProvider;
    }
}

/// <summary>
/// Reponsible for implementing the IDynamicHost when the
/// Host property is not available on the TextTemplating type. The Host
/// property only exists when the hostspecific attribute of the template
/// directive is set to true.
/// </summary>
public class NullHost : IDynamicHost
{
    /// <summary>
    /// An abstraction of the call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost ResolveParameterValue
    /// that simply retuns null.
    /// </summary>
    public string ResolveParameterValue(string id, string name, string otherName)
    {
        return null;
    }

    /// <summary>
    /// An abstraction of the call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost ResolvePath
    /// that simply retuns the path passed in.
    /// </summary>
    public string ResolvePath(string path)
    {
        return path;
    }

    /// <summary>
    /// An abstraction of the call to Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost TemplateFile
    /// that returns null.
    /// </summary>
    public string TemplateFile
    {
        get
        {
            return null;
        }
    }

    /// <summary>
    /// Returns null.
    /// </summary>
    public IServiceProvider AsIServiceProvider()
    {
        return null;
    }
}
}
