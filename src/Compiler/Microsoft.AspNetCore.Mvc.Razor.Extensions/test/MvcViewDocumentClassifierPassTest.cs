﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class MvcViewDocumentClassifierPassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_3_0;

    [Fact]
    public void MvcViewDocumentClassifierPass_SetsDocumentKind()
    {
        // Arrange
        var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("some-content", "Test.cshtml"));

        var projectEngine = CreateProjectEngine();
        var irDocument = CreateIRDocument(projectEngine, codeDocument);
        var pass = new MvcViewDocumentClassifierPass
        {
            Engine = projectEngine.Engine
        };

        // Act
        pass.Execute(codeDocument, irDocument);

        // Assert
        Assert.Equal("mvc.1.0.view", irDocument.DocumentKind);
    }

    [Fact]
    public void MvcViewDocumentClassifierPass_NoOpsIfDocumentKindIsAlreadySet()
    {
        // Arrange
        var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("some-content", "Test.cshtml"));

        var projectEngine = CreateProjectEngine();
        var irDocument = CreateIRDocument(projectEngine, codeDocument);
        irDocument.DocumentKind = "some-value";
        var pass = new MvcViewDocumentClassifierPass
        {
            Engine = projectEngine.Engine
        };

        // Act
        pass.Execute(codeDocument, irDocument);

        // Assert
        Assert.Equal("some-value", irDocument.DocumentKind);
    }

    [Fact]
    public void MvcViewDocumentClassifierPass_SetsNamespace()
    {
        // Arrange
        var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("some-content", "Test.cshtml"));

        var projectEngine = CreateProjectEngine();
        var irDocument = CreateIRDocument(projectEngine, codeDocument);
        var pass = new MvcViewDocumentClassifierPass
        {
            Engine = projectEngine.Engine
        };

        // Act
        pass.Execute(codeDocument, irDocument);
        var visitor = new Visitor();
        visitor.Visit(irDocument);

        // Assert
        Assert.Equal("AspNetCore", visitor.Namespace.Content);
    }

    [Fact]
    public void MvcViewDocumentClassifierPass_SetsClass()
    {
        // Arrange
        var properties = RazorSourceDocumentProperties.Create(filePath: "ignored", relativePath: "Test.cshtml");
        var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("some-content", properties));

        var projectEngine = CreateProjectEngine();
        var irDocument = CreateIRDocument(projectEngine, codeDocument);
        var pass = new MvcViewDocumentClassifierPass
        {
            Engine = projectEngine.Engine
        };

        // Act
        pass.Execute(codeDocument, irDocument);
        var visitor = new Visitor();
        visitor.Visit(irDocument);

        // Assert
        var baseNode = Assert.IsType<BaseTypeWithModel>(visitor.Class.BaseType);
        Assert.Equal("global::Microsoft.AspNetCore.Mvc.Razor.RazorPage", baseNode.BaseType.Content);
        Assert.Equal("TModel", baseNode.ModelType.Content);
        Assert.Equal(new[] { "public" }, visitor.Class.Modifiers);
        Assert.Equal("Test", visitor.Class.ClassName);
    }

    [Fact]
    public void MvcViewDocumentClassifierPass_NullFilePath_SetsClass()
    {
        // Arrange
        var properties = RazorSourceDocumentProperties.Create(filePath: null, relativePath: null);
        var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("some-content", properties));

        var projectEngine = CreateProjectEngine();
        var irDocument = CreateIRDocument(projectEngine, codeDocument);
        var pass = new MvcViewDocumentClassifierPass
        {
            Engine = projectEngine.Engine
        };

        // Act
        pass.Execute(codeDocument, irDocument);
        var visitor = new Visitor();
        visitor.Visit(irDocument);

        // Assert
        var baseNode = Assert.IsType<BaseTypeWithModel>(visitor.Class.BaseType);
        Assert.Equal("global::Microsoft.AspNetCore.Mvc.Razor.RazorPage", baseNode.BaseType.Content);
        Assert.Equal("TModel", baseNode.ModelType.Content);
        Assert.Equal(new[] { "public" }, visitor.Class.Modifiers);
        AssertEx.Equal("AspNetCore_ec563e63d931b806184cb02f79875e4f3b21d1ca043ad06699424459128b58c0", visitor.Class.ClassName);
    }

    [Theory]
    [InlineData("/Views/Home/Index.cshtml", "_Views_Home_Index")]
    [InlineData("/Areas/MyArea/Views/Home/About.cshtml", "_Areas_MyArea_Views_Home_About")]
    public void MvcViewDocumentClassifierPass_UsesRelativePathToGenerateTypeName(string relativePath, string expected)
    {
        // Arrange
        var properties = RazorSourceDocumentProperties.Create(filePath: "ignored", relativePath: relativePath);
        var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("some-content", properties));

        var projectEngine = CreateProjectEngine();
        var irDocument = CreateIRDocument(projectEngine, codeDocument);
        var pass = new MvcViewDocumentClassifierPass
        {
            Engine = projectEngine.Engine
        };

        // Act
        pass.Execute(codeDocument, irDocument);
        var visitor = new Visitor();
        visitor.Visit(irDocument);

        // Assert
        Assert.Equal(expected, visitor.Class.ClassName);
    }

    [Fact]
    public void MvcViewDocumentClassifierPass_UsesAbsolutePath_IfRelativePathIsNotSet()
    {
        // Arrange
        var properties = RazorSourceDocumentProperties.Create(filePath: @"x::\application\Views\Home\Index.cshtml", relativePath: null);
        var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("some-content", properties));

        var projectEngine = CreateProjectEngine();
        var irDocument = CreateIRDocument(projectEngine, codeDocument);
        var pass = new MvcViewDocumentClassifierPass
        {
            Engine = projectEngine.Engine
        };

        // Act
        pass.Execute(codeDocument, irDocument);
        var visitor = new Visitor();
        visitor.Visit(irDocument);

        // Assert
        Assert.Equal("x___application_Views_Home_Index", visitor.Class.ClassName);
    }

    [Fact]
    public void MvcViewDocumentClassifierPass_SanitizesClassName()
    {
        // Arrange
        var properties = RazorSourceDocumentProperties.Create(filePath: @"x:\Test.cshtml", relativePath: "path.with+invalid-chars");
        var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("@page", properties));

        var projectEngine = CreateProjectEngine();
        var irDocument = CreateIRDocument(projectEngine, codeDocument);
        var pass = new MvcViewDocumentClassifierPass
        {
            Engine = projectEngine.Engine
        };

        // Act
        pass.Execute(codeDocument, irDocument);
        var visitor = new Visitor();
        visitor.Visit(irDocument);

        // Assert
        Assert.Equal("path_with_invalid_chars", visitor.Class.ClassName);
    }

    [Fact]
    public void MvcViewDocumentClassifierPass_SetsUpExecuteAsyncMethod()
    {
        // Arrange
        var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("some-content", "Test.cshtml"));

        var projectEngine = CreateProjectEngine();
        var irDocument = CreateIRDocument(projectEngine, codeDocument);
        var pass = new MvcViewDocumentClassifierPass
        {
            Engine = projectEngine.Engine
        };

        // Act
        pass.Execute(codeDocument, irDocument);
        var visitor = new Visitor();
        visitor.Visit(irDocument);

        // Assert
        Assert.Equal("ExecuteAsync", visitor.Method.MethodName);
        Assert.Equal("global::System.Threading.Tasks.Task", visitor.Method.ReturnType);
        Assert.Equal(new[] { "public", "async", "override" }, visitor.Method.Modifiers);
    }

    private static DocumentIntermediateNode CreateIRDocument(RazorProjectEngine projectEngine, RazorCodeDocument codeDocument)
    {
        foreach (var phase in projectEngine.Phases)
        {
            phase.Execute(codeDocument);

            if (phase is IRazorIntermediateNodeLoweringPhase)
            {
                break;
            }
        }

        return codeDocument.GetDocumentIntermediateNode();
    }

    private class Visitor : IntermediateNodeWalker
    {
        public NamespaceDeclarationIntermediateNode Namespace { get; private set; }

        public ClassDeclarationIntermediateNode Class { get; private set; }

        public MethodDeclarationIntermediateNode Method { get; private set; }

        public override void VisitMethodDeclaration(MethodDeclarationIntermediateNode node)
        {
            Method = node;
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node)
        {
            Namespace = node;
            base.VisitNamespaceDeclaration(node);
        }

        public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
        {
            Class = node;
            base.VisitClassDeclaration(node);
        }
    }
}
