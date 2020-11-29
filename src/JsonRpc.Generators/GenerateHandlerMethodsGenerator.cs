using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.JsonRpc.Generators.Cache;
using OmniSharp.Extensions.JsonRpc.Generators.Contexts;
using OmniSharp.Extensions.JsonRpc.Generators.Strategies;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static OmniSharp.Extensions.JsonRpc.Generators.Helpers;

namespace OmniSharp.Extensions.JsonRpc.Generators
{
    [Generator]
    public class GenerateHandlerMethodsGenerator : CachedSourceGenerator<GenerateHandlerMethodsGenerator.SyntaxReceiver, TypeDeclarationSyntax>
    {
        protected override void Execute(GeneratorExecutionContext context, SyntaxReceiver syntaxReceiver, AddCacheSource<TypeDeclarationSyntax> addCacheSource, ReportCacheDiagnostic<TypeDeclarationSyntax> cacheDiagnostic)
        {
            foreach (var candidateClass in syntaxReceiver.Candidates)
            {
//                context.ReportDiagnostic(Diagnostic.Create(GeneratorDiagnostics.Message, null, $"candidate: {candidateClass.Identifier.ToFullString()}"));
                // can this be async???
                context.CancellationToken.ThrowIfCancellationRequested();

                var additionalUsings = new HashSet<string> {
                    "System",
                    "System.Collections.Generic",
                    "System.Threading",
                    "System.Threading.Tasks",
                    "MediatR",
                    "Microsoft.Extensions.DependencyInjection"
                };

                GeneratorData? actionItem = null;

                try
                {
                    actionItem = GeneratorData.Create(context, candidateClass, addCacheSource, cacheDiagnostic, additionalUsings);
                }
                catch (Exception e)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GeneratorDiagnostics.Exception, candidateClass.GetLocation(), e.Message, e.StackTrace ?? string.Empty));
                    Debug.WriteLine(e);
                    Debug.WriteLine(e.StackTrace);
                }

                if (actionItem is null) continue;

                var members = CompilationUnitGeneratorStrategies.Aggregate(
                    new List<MemberDeclarationSyntax>(), (m, strategy) => {
                        try
                        {
                            m.AddRange(strategy.Apply(actionItem));
                        }
                        catch (Exception e)
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    GeneratorDiagnostics.Exception, candidateClass.GetLocation(), $"Strategy {strategy.GetType().FullName} failed!" + " - " + e.Message,
                                    e.StackTrace ?? string.Empty
                                )
                            );
                            Debug.WriteLine($"Strategy {strategy.GetType().FullName} failed!");
                            Debug.WriteLine(e);
                            Debug.WriteLine(e.StackTrace);
                        }

                        return m;
                    }
                );

                if (!members.Any()) continue;

                var existingUsings = candidateClass.SyntaxTree.GetCompilationUnitRoot()
                                                   .Usings
                                                   .Select(x => x.WithoutTrivia())
                                                   .Union(
                                                        additionalUsings
                                                           .Except(
                                                                candidateClass.SyntaxTree.GetCompilationUnitRoot()
                                                                              .Usings
                                                                              .Where(z => z.Alias == null)
                                                                              .Select(z => z.Name.ToFullString())
                                                            )
                                                           .Distinct()
                                                           .Select(z => UsingDirective(IdentifierName(z)))
                                                    )
                                                   .OrderBy(x => x.Name.ToFullString())
                                                   .ToImmutableArray();

                var cu = CompilationUnit(
                             List<ExternAliasDirectiveSyntax>(),
                             List(existingUsings),
                             List<AttributeListSyntax>(),
                             List(members)
                         )
                        .WithLeadingTrivia(Comment(Preamble.GeneratedByATool))
                        .WithTrailingTrivia(CarriageReturnLineFeed);

                addCacheSource(
                    $"{candidateClass.Identifier.Text}{( candidateClass.Arity > 0 ? candidateClass.Arity.ToString() : "" )}.cs",
                    candidateClass,
                    cu.NormalizeWhitespace().GetText(Encoding.UTF8)
                );
            }
        }

        private static readonly ImmutableArray<ICompilationUnitGeneratorStrategy> CompilationUnitGeneratorStrategies = GetCompilationUnitGeneratorStrategies();

        private static ImmutableArray<ICompilationUnitGeneratorStrategy> GetCompilationUnitGeneratorStrategies()
        {
            var actionContextStrategies = ImmutableArray.Create<IExtensionMethodContextGeneratorStrategy>(
                new WarnIfResponseRouterIsNotProvidedStrategy(),
                new OnNotificationMethodGeneratorWithoutRegistrationOptionsStrategy(),
                new OnNotificationMethodGeneratorWithRegistrationOptionsStrategy(),
                new OnRequestMethodGeneratorWithoutRegistrationOptionsStrategy(false),
                new OnRequestMethodGeneratorWithoutRegistrationOptionsStrategy(true),
                new OnRequestTypedResolveMethodGeneratorWithoutRegistrationOptionsStrategy(),
                new OnRequestMethodGeneratorWithRegistrationOptionsStrategy(false),
                new OnRequestMethodGeneratorWithRegistrationOptionsStrategy(true),
                new OnRequestTypedResolveMethodGeneratorWithRegistrationOptionsStrategy(),
                new SendMethodNotificationStrategy(),
                new SendMethodRequestStrategy()
            );
            var actionStrategies = ImmutableArray.Create<IExtensionMethodGeneratorStrategy>(
                new EnsureNamespaceStrategy(),
                new HandlerRegistryActionContextRunnerStrategy(actionContextStrategies),
                new RequestProxyActionContextRunnerStrategy(actionContextStrategies),
                new TypedDelegatingHandlerStrategy()
            );
            var compilationUnitStrategies = ImmutableArray.Create<ICompilationUnitGeneratorStrategy>(
                new HandlerGeneratorStrategy(),
                new ExtensionMethodGeneratorStrategy(actionStrategies)
            );
            return compilationUnitStrategies;
        }

        public GenerateHandlerMethodsGenerator() : base(() => new SyntaxReceiver(Cache)) { }
        public static CacheContainer<TypeDeclarationSyntax> Cache = new ();
        public class SyntaxReceiver : SyntaxReceiverCache<TypeDeclarationSyntax>
        {
            private string _attributes;
            public List<TypeDeclarationSyntax> Candidates { get; } = new();

            public SyntaxReceiver(CacheContainer<TypeDeclarationSyntax> cache) : base(cache)
            {
                _attributes = "GenerateHandler,GenerateRequestMethods,GenerateHandlerMethods";
            }

            public override string? GetKey(TypeDeclarationSyntax syntax)
            {
                var hasher = new CacheKeyHasher();
                hasher.Append(syntax.SyntaxTree.FilePath);
                hasher.Append(syntax.Identifier.Text);
                hasher.Append(syntax.TypeParameterList);
                hasher.Append(syntax.AttributeLists);
                hasher.Append(syntax.BaseList);
                return hasher;
            }

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public override void OnVisitNode(TypeDeclarationSyntax syntaxNode)
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is ( ClassDeclarationSyntax { } or InterfaceDeclarationSyntax { }) && syntaxNode.AttributeLists.ContainsAttribute(_attributes))
                {
                    Candidates.Add(syntaxNode);
                }
            }
        }
    }
}
