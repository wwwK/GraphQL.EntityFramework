﻿using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.EntityFramework
{
    public static partial class ObjectGraphExtension
    {
        public static FieldType AddQueryField<TReturn>(
            this ObjectGraphType graph,
            Type graphType,
            string name,
            Func<ResolveFieldContext<object>, IQueryable<TReturn>> resolve,
            string includeName = null)
            where TReturn : class
        {
            var field = BuildQueryField(graphType, name, resolve, includeName);
            return graph.AddField(field);
        }

        public static FieldType AddQueryField<TSource, TReturn>(
            this ObjectGraphType graph,
            Type graphType,
            string name,
            Func<ResolveFieldContext<TSource>, IQueryable<TReturn>> resolve,
            string includeName = null)
            where TReturn : class
        {
            var field = BuildQueryField(graphType, name, resolve, includeName);
            return graph.AddField(field);
        }

        static FieldType BuildQueryField<TSource, TReturn>(
            Type graphType,
            string name,
            Func<ResolveFieldContext<TSource>, IQueryable<TReturn>> resolve,
            string includeName)
            where TReturn : class
        {
            var listGraphType = MakeListGraphType(graphType);
            return BuildQueryField(name, resolve, includeName, listGraphType);
        }

        public static FieldType AddQueryField<TGraph, TReturn>(
            this ObjectGraphType graph,
            string name,
            Func<ResolveFieldContext<object>, IQueryable<TReturn>> resolve,
            string includeName = null)
            where TGraph : ObjectGraphType<TReturn>, IGraphType
            where TReturn : class
        {
            var field = BuildQueryField<object, TGraph, TReturn>(name, resolve, includeName);
            return graph.AddField(field);
        }

        public static FieldType AddQueryField<TSource, TGraph, TReturn>(
            this ObjectGraphType graph,
            string name,
            Func<ResolveFieldContext<TSource>, IQueryable<TReturn>> resolve,
            string includeName = null)
            where TGraph : ObjectGraphType<TReturn>, IGraphType
            where TReturn : class
        {
            var field = BuildQueryField<TSource, TGraph, TReturn>(name, resolve, includeName);
            return graph.AddField(field);
        }

        static FieldType BuildQueryField<TSource, TGraph, TReturn>(
            string name,
            Func<ResolveFieldContext<TSource>, IQueryable<TReturn>> resolve,
            string includeName)
            where TGraph : ObjectGraphType<TReturn>, IGraphType
            where TReturn : class
        {
            var listGraphType = MakeListGraphType(typeof(TGraph));
            return BuildQueryField(name, resolve, includeName, listGraphType);
        }

        static FieldType BuildQueryField<TSource, TReturn>(
            string name,
            Func<ResolveFieldContext<TSource>, IQueryable<TReturn>> resolve,
            string includeName,
            Type listGraphType)
            where TReturn : class
        {
            return new FieldType
            {
                Name = name,
                Type = listGraphType,
                Arguments = ArgumentAppender.GetQueryArguments(),
                Metadata = IncludeAppender.GetIncludeMetadata(includeName),
                Resolver = new AsyncFieldResolver<TSource, List<TReturn>>(
                    async context =>
                    {
                        var returnTypes = resolve(context);
                        return await
                            IncludeAppender.AddIncludes(returnTypes, context)
                                .ApplyGraphQlArguments(context)
                                .ToListAsync()
                                .ConfigureAwait(false);
                    })
            };
        }
    }
}