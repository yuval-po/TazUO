using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HandlebarsDotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EventGenerator;

using ClassMethodsGroup = IGrouping<(string Namespace, string ClassName), ApiEventInfo>;

[Generator]
public class EventSourceGenerator : IIncrementalGenerator
{
    private static readonly Dictionary<string, INamedTypeSymbol> _qualifiedTypeToSymbol = new();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find methods with the attribute
        var methodsWithAttribute = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (s, _) => WithExceptionHandling(() => IsSyntaxTargetForGeneration(s)),
                static (ctx, _) => WithExceptionHandling(() => GetSemanticTargetForGeneration(ctx)))
            .Where(static m => m is not null);

        // Combine with compilation
        var compilationAndMethods =
            WithExceptionHandling(() => context.CompilationProvider.Combine(methodsWithAttribute.Collect()));

        // Generate the source
        context.RegisterSourceOutput(compilationAndMethods,
            static (spc, source) => WithExceptionHandling(() => Execute(source.Left, source.Right, spc)));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is EventFieldDeclarationSyntax { AttributeLists.Count: > 0 };
    }

    private static ApiEventInfo GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var eventFieldDecl = (EventFieldDeclarationSyntax)context.Node;
        var (nsName, className) = AssertGetNamespaceAndClassName(eventFieldDecl);

        foreach (var attrList in eventFieldDecl.AttributeLists)
        {
            var apiAttr = attrList.Attributes.FirstOrDefault(attr => attr.Name.GetText().ToString() == "ApiEvent");
            if (apiAttr == null)
                continue;

            var varDecl = attrList.Parent?.ChildNodes()?.FirstOrDefault(c => c is VariableDeclarationSyntax);

            var declarator = varDecl?.ChildNodes().FirstOrDefault(c => c is VariableDeclaratorSyntax);
            if (declarator == null)
                continue;

            var eventName = declarator.GetText().ToString().Trim();
            if (string.IsNullOrWhiteSpace(eventName))
                continue;

            // Get the event args type from EventSink
            var eventArgsType = GetEventArgsType(
                context.SemanticModel.Compilation,
                "ClassicUO.Game.Managers.EventSink",
                eventName
            );

            if (string.IsNullOrEmpty(eventArgsType))
                continue;

            return new ApiEventInfo
            {
                EventName = eventName,
                EventArgsType = eventArgsType,
                ClassName = className,
                Namespace = nsName
            };
        }

        return null;
    }

    private static (string Namespace, string ClassName) AssertGetNamespaceAndClassName(
        EventFieldDeclarationSyntax eventFieldDecl)
    {
        var parentClass = (ClassDeclarationSyntax)eventFieldDecl.Parent;

        if (parentClass == null)
            throw new InvalidOperationException("Failed to determine parent class during code generation");

        var parentClassName = parentClass.Identifier.Text;

        return parentClass.Parent switch
        {
            FileScopedNamespaceDeclarationSyntax ns => (ns.Name.ToString(), parentClassName),
            NamespaceDeclarationSyntax ns => (ns.Name.ToString(), parentClassName),
            _ => throw new InvalidOperationException("Failed to determine namespace during code generation")
        };
    }

    private static string GetEventArgsType(
        Compilation compilation,
        string sourceClassFullyQualifiedName,
        string eventName
    )
    {
        if (!_qualifiedTypeToSymbol.TryGetValue(sourceClassFullyQualifiedName, out var namedSymbol))
        {
            namedSymbol = compilation.GetTypeByMetadataName(sourceClassFullyQualifiedName);
            _qualifiedTypeToSymbol.Add(sourceClassFullyQualifiedName, namedSymbol);
        }

        var eventMember = namedSymbol?.GetMembers(eventName).OfType<IEventSymbol>().FirstOrDefault();
        if (eventMember?.Type is not INamedTypeSymbol { IsGenericType: true } namedType)
            return "object";

        var typeArg = namedType.TypeArguments.FirstOrDefault();
        return typeArg?.ToDisplayString() ?? "object";
    }

    private static void Execute(Compilation _, IEnumerable<ApiEventInfo> methods, SourceProductionContext context)
    {
        WithExceptionHandling(() =>
        {
            var methodsArray = methods.ToArray() ?? [];
            var methodsByClass = methodsArray.GroupBy(m => (m.Namespace, m.ClassName));

            var template = GetTemplate("ApiCallbackDispatcher.hbs");
            var handlebars = Handlebars.Create();
            handlebars.RegisterHelper("TrimOnPrefix", TrimOnPrefix);
            var compiledTemplate = handlebars.Compile(template);

            foreach (var group in methodsByClass)
            {
                var templateCtx = CreateContext(group);
                var source = compiledTemplate(templateCtx);
                context.AddSource($"{group.Key.ClassName}.Api.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        });
    }

    private static void TrimOnPrefix(
        EncodedTextWriter output,
        Context _,
        Arguments arguments
    )
    {
        if (arguments.Length != 1 || arguments[0] is not string)
            throw new ArgumentException("TrimOnPrefix helper requires exactly one string parameter");

        var asStr = (string)arguments[0];
        if (asStr.StartsWith("On", StringComparison.InvariantCultureIgnoreCase))
            asStr = asStr.Substring(2);
        output.WriteSafeString(asStr);
    }

    private static TemplateContext CreateContext(ClassMethodsGroup flatInfos)
    {
        var methodArray = flatInfos.ToArray();
        var methods = methodArray.Select(m => new EventInfoSlim
        {
            Name = m.EventName,
            EventArgsType = m.EventArgsType
        }).ToArray();

        var genClass = new GeneratedClassSlim
        {
            Name = $"{flatInfos.Key.ClassName}Api",
            Namespace = flatInfos.Key.Namespace,
            GeneratedFromClassName = flatInfos.Key.ClassName,
            Events = methods.ToArray()
        };

        return new TemplateContext { Classes = [genClass] };
    }

    private static string GetTemplate(string templateName)
    {
        var info = Assembly.GetExecutingAssembly().GetName();
        var name = info.Name;
        using var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream($"{name}.Templates.{templateName}");

        if (stream == null)
            throw new InvalidDataException("Could not obtain resource stream during code emission");

        using var streamReader = new StreamReader(stream, Encoding.UTF8);
        return streamReader.ReadToEnd();
    }

    private static void WithExceptionHandling(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            Log(e.Message);
            throw;
        }
    }

    private static T WithExceptionHandling<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (Exception e)
        {
            Log(e.Message);
            throw;
        }
    }

    private static void Log(string msg)
    {
        try
        {
#pragma warning disable RS1035
            File.AppendAllText(Path.Combine("/mnt/7e91759c-6dd7-4c99-8d38-e6422452a469/git/TazUO/eventgen.log"),
                $"[{DateTime.UtcNow}] {msg}" + "\n");
#pragma warning restore RS1035
        }
        catch
        {
        }
    }
}

internal class TemplateContext
{
    public GeneratedClassSlim[] Classes { get; set; }
}

internal class GeneratedClassSlim
{
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string GeneratedFromClassName { get; set; }
    public EventInfoSlim[] Events { get; set; }
}

internal class EventInfoSlim
{
    public string Name { get; set; }
    public string EventArgsType { get; set; }
}

internal class ApiEventInfo
{
    public string EventName { get; set; }
    public string EventArgsType { get; set; }
    public string ClassName { get; set; }
    public string Namespace { get; set; }
}