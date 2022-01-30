﻿using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace FastEndpoints.Swagger;

internal class DefaultDocumentProcessor : IDocumentProcessor
{
    private readonly int maxEpVer;
    public DefaultDocumentProcessor(int maxEndpointVersion) => maxEpVer = maxEndpointVersion;

    public void Process(DocumentProcessorContext ctx)
    {
        var pathItems = ctx.Document.Paths
            .SelectMany(p => p.Value.Values)
            .Select(o =>
            {
                var tag = o.Tags.SingleOrDefault(t => t.StartsWith("|"));
                var segments = tag?.Split("|");
                return new
                {
                    route = segments?[1],
                    ver = Convert.ToInt32(segments?[2]),
                    depVer = Convert.ToInt32(segments?[3]),
                    pathItm = o.Parent
                };
            })
            .GroupBy(x => x.route)
            .Select(g => new
            {
                pathItm = g.Where(x => x.ver <= maxEpVer)
                           .OrderByDescending(x => x.ver)
                           .Take(1)
                           .Where(x => x.depVer == 0 || x.depVer > maxEpVer)
                           .Select(x => x.pathItm)
            })
            .SelectMany(x => x.pathItm)
            .ToArray();

        foreach (var p in ctx.Document.Paths)
        {
            if (!pathItems.Contains(p.Value))
                ctx.Document.Paths.Remove(p.Key);

            foreach (var op in p.Value.Values)
                op.Tags.Remove(op.Tags.SingleOrDefault(t => t.StartsWith("|")));
        }
    }
}