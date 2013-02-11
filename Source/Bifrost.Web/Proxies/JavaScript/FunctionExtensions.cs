﻿using System;
using System.Linq;

namespace Bifrost.Web.Proxies.JavaScript
{
    public static class FunctionExtensions
    {
        public static Function WithParameters(this Function function, params string[] parameters)
        {
            function.Parameters = parameters;
            return function;
        }

        public static FunctionBody Property(this FunctionBody functionBody, string name, Action<PropertyAssignment> callback)
        {
            var propertyAssignment = new PropertyAssignment(name);
            functionBody.AddChild(propertyAssignment);
            callback(propertyAssignment);
            return functionBody;
        }

        public static FunctionBody Variant(this FunctionBody functionBody, string name, Action<VariantAssignment> callback)
        {
            var variantAssignment = new VariantAssignment(name);
            functionBody.AddChild(variantAssignment);
            callback(variantAssignment);
            return functionBody;
        }



        public static FunctionBody Scope(this FunctionBody functionBody, string name, Action<Scope> callback)
        {
            var scope = new Scope(name);
            functionBody.AddChild(scope);
            callback(scope);
            return functionBody;
        }

        public static FunctionCall WithParameters(this FunctionCall functionCall, params string[] parameters)
        {
            functionCall.Parameters = parameters.Select(p=>new Literal(p)).ToArray();
            return functionCall;
        }

        public static FunctionCall WithParameters(this FunctionCall functionCall, params LanguageElement[] parameters)
        {
            functionCall.Parameters = parameters;
            return functionCall;
        }

        public static FunctionCall WithName(this FunctionCall functionCall, string name)
        {
            functionCall.Function = name;
            return functionCall;
        }
    }
}