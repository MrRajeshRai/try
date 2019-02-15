﻿using System;
using System.Collections.Generic;
using System.Linq;
using Markdig;
using MLS.Agent.Markdown;
using Recipes;
using WorkspaceServer;

namespace MLS.Agent
{
    public class MarkdownProject
    {
        internal IDirectoryAccessor DirectoryAccessor { get; }
        private readonly PackageRegistry _packageRegistry;

        private readonly Dictionary<RelativeFilePath, MarkdownPipeline> _markdownPipelines = new Dictionary<RelativeFilePath, MarkdownPipeline>();

        public MarkdownProject(IDirectoryAccessor directoryAccessor, PackageRegistry packageRegistry)
        {
            DirectoryAccessor = directoryAccessor ?? throw new ArgumentNullException(nameof(directoryAccessor));
            _packageRegistry = packageRegistry ?? throw new ArgumentNullException(nameof(packageRegistry));
        }

        public IEnumerable<MarkdownFile> GetAllMarkdownFiles() =>
            DirectoryAccessor.GetAllFilesRecursively()
                             .Where(file => file.Extension == ".md")
                             .Select(file => new MarkdownFile(file, this));

        public bool TryGetMarkdownFile(RelativeFilePath path, out MarkdownFile markdownFile)
        {
            if (!DirectoryAccessor.FileExists(path))
            {
                markdownFile = null;
                return false;
            }

            markdownFile = new MarkdownFile(path, this);
            return true;
        }

        internal MarkdownPipeline GetMarkdownPipelineFor(RelativeFilePath filePath)
        {
            return _markdownPipelines.GetOrAdd(filePath, key =>
            {
                var relativeAccessor = DirectoryAccessor.GetDirectoryAccessorForRelativePath(filePath.Directory);

                return new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .UseCodeLinks(relativeAccessor, _packageRegistry)
                    .Build();
            });
        }
    }
}