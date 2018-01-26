﻿using System.Linq;
using Newtonsoft.Json;
using Xunit;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.Examples;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Shouldly;
using System;

namespace Swashbuckle.AspNetCore.SwaggerGen.Test
{
    public class ExamplesOperationFilterTests
    {
        private readonly IOperationFilter sut;

        public ExamplesOperationFilterTests()
        {
            var mvcJsonOptions = new MvcJsonOptions();
            var options = Options.Create(mvcJsonOptions);
            sut = new ExamplesOperationFilter(options);
        }

        [Fact]
        public void Apply_SetsResponseExamples_FromAttributes()
        {
            // Arrange
            var operation = new Operation { OperationId = "foobar" };
            var filterContext = FilterContextFor(nameof(FakeActions.AnnotatedWithSwaggerResponseExampleAttributes));
            SetSwaggerResponsesOnOperation(operation, filterContext);

            // Act
            sut.Apply(operation, filterContext);

            // Assert
            var examples = (JObject)operation.Responses["200"].Examples;
            var actualExample = examples["application/json"];

            var expectedExample = (PersonResponse)new PersonResponseExample().GetExamples();
            actualExample["id"].ShouldBe(expectedExample.Id);
            actualExample["first"].ShouldBe(expectedExample.FirstName);
        }

        [Fact]
        public void Apply_SetsRequestExamples_FromAttributes()
        {
            // Arrange
            var operation = new Operation { OperationId = "foobar", Parameters = new[] { new BodyParameter { In = "body", Schema = new Schema { Ref = "#/definitions/PersonRequest" } } } };
            var filterContext = FilterContextFor(nameof(FakeActions.AnnotatedWithSwaggerRequestExampleAttributes));

            // Act
            sut.Apply(operation, filterContext);

            // Assert
            var actualExample = (JObject)filterContext.SchemaRegistry.Definitions["PersonRequest"].Example;
            var expectedExample = (PersonRequest)new PersonRequestExample().GetExamples();
            actualExample["title"].ShouldBe(expectedExample.Title.ToString());
            actualExample["firstName"].ShouldBe(expectedExample.FirstName);
            actualExample["age"].ShouldBe(expectedExample.Age);
        }

        private void SetSwaggerResponsesOnOperation(Operation operation, OperationFilterContext filterContext)
        {
            var swaggerResponseFilter = new SwaggerResponseAttributeFilter();
            swaggerResponseFilter.Apply(operation, filterContext);
        }

        private OperationFilterContext FilterContextFor(
            string actionFixtureName,
            string controllerFixtureName = "NotAnnotated")
        {
            var fakeProvider = new FakeApiDescriptionGroupCollectionProvider();
            var apiDescription = fakeProvider
                .Add("GET", "collection", actionFixtureName, controllerFixtureName)
                .ApiDescriptionGroups.Items.First()
                .Items.First();

            return new OperationFilterContext(
                apiDescription,
                new SchemaRegistry(new JsonSerializerSettings()));
        }
    }
}
