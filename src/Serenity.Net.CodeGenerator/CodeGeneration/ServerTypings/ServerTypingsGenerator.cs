namespace Serenity.CodeGeneration;

#if ISSOURCEGENERATOR
public partial class ServerTypingsGenerator(Microsoft.CodeAnalysis.Compilation compilation,
    System.Threading.CancellationToken cancellationToken)
    : TypingsGeneratorBase(compilation, cancellationToken)
{
#else
public partial class ServerTypingsGenerator : TypingsGeneratorBase
{
    public ServerTypingsGenerator(IFileSystem fileSystem, params Assembly[] assemblies)
        : base(fileSystem, assemblies)
    {
    }

    public ServerTypingsGenerator(IFileSystem fileSystem, params string[] assemblyLocations)
        : base(fileSystem, assemblyLocations)
    {
    }
#endif

    public bool LocalTexts { get; set; }
    public bool ModuleReExports { get; set; } = true;
    public bool ModuleTypings { get; set; } = false;
    public bool NamespaceTypings { get; set; } = true;

    public readonly HashSet<string> LocalTextFilters = [];

    protected override void GenerateAll()
    {
        base.GenerateAll();

        if (LocalTexts && NamespaceTypings)
            GenerateTexts(module: false);

        if (LocalTexts && ModuleTypings)
            GenerateTexts(module: true);

        if (ModuleTypings && ModuleReExports)
            GenerateModuleReExports();
    }

    protected override void GenerateCodeFor(TypeDefinition type)
    {
        void add(Action<TypeDefinition, bool> action, string fileIdentifier = null)
        {
            var typeNamespace = GetNamespace(type);

            if (NamespaceTypings)
            {
                cw.Indented("namespace ");
                sb.Append(typeNamespace);
                cw.InBrace(delegate
                {
                    action(type, false);
                });

                var fileName = GetFileNameFor(typeNamespace, fileIdentifier ?? type.Name, module: false) + ".ts";
                AddFile(fileName, module: false);
            }

            if (ModuleTypings)
            {
                action(type, true);
                var fileName = GetFileNameFor(typeNamespace, fileIdentifier ?? type.Name, module: true) + ".ts";
                AddFile(fileName, module: true);
            }
        }

        if (type.IsEnum())
        {
            add(GenerateEnum);
            return;
        }

        if (TypingsUtils.IsSubclassOf(type, "Microsoft.AspNetCore.Mvc", "ControllerBase") ||
            TypingsUtils.IsSubclassOf(type, "System.Web.Mvc", "Controller"))
        {
            var controllerIdentifier = GetControllerIdentifier(type);
            add((t, m) => GenerateService(t, controllerIdentifier, m), controllerIdentifier);
            return;
        }

        var formScriptAttr = TypingsUtils.GetAttr(type, "Serenity.ComponentModel", "FormScriptAttribute");
        if (formScriptAttr != null)
        {
            var formIdentifier = type.Name;
            var isServiceRequest = TypingsUtils.IsSubclassOf(type, "Serenity.Services", "ServiceRequest");
            if (formIdentifier.EndsWith(requestSuffix, StringComparison.Ordinal) && isServiceRequest)
                formIdentifier = formIdentifier[..^requestSuffix.Length] + "Form";

            add((t, m) => GenerateForm(t, formScriptAttr, formIdentifier, m), formIdentifier);

            EnqueueTypeMembers(type);

            if (isServiceRequest)
                add(GenerateBasicType);

            return;
        }

        var columnsScriptAttr = TypingsUtils.GetAttr(type, "Serenity.ComponentModel", "ColumnsScriptAttribute");
        if (columnsScriptAttr != null)
        {
            add((t, m) => GenerateColumns(t, columnsScriptAttr, m));
            EnqueueTypeMembers(type);
            return;
        }

        if (TypingsUtils.GetAttr(type, "Serenity.Extensibility", "NestedPermissionKeysAttribute") != null ||
            TypingsUtils.GetAttr(type, "Serenity.ComponentModel", "NestedPermissionKeysAttribute") != null)
        {
            add(GeneratePermissionKeys);
            return;
        }

        if (TypingsUtils.IsSubclassOf(type, "Serenity.Data", "Row") ||
            TypingsUtils.IsSubclassOf(type, "Serenity.Data", "Row`1"))
        {
            var metadata = ExtractRowMetadata(type);
            
            add((t, m) =>
            {
                GenerateRowType(t, m);
                GenerateRowMetadata(t, metadata, m);
            });
            
            return;
        }

        add(GenerateBasicType);
    }

    public void SetLocalTextFiltersFrom(IFileSystem fileSystem, string appSettingsFile)
    {
        if (!LocalTexts || !fileSystem.FileExists(appSettingsFile))
            return;

        try
        {
            var obj = Newtonsoft.Json.Linq.JObject.Parse(fileSystem.ReadAllText(appSettingsFile));
            if ((obj["LocalTextPackages"] ?? ((obj["AppSettings"] 
                as Newtonsoft.Json.Linq.JObject)?["LocalTextPackages"])) is 
                Newtonsoft.Json.Linq.JObject packages)
            {
                foreach (var p in packages.PropertyValues())
                    foreach (var x in p.Values<string>())
                        LocalTextFilters.Add(x);
            }
        }
        catch
        {
        }
    }
}