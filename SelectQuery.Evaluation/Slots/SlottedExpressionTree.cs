using System;
using System.Collections.Generic;
using System.Linq;
using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Evaluation.Slots
{
    // TODO check case-sensitivity
    internal class SlottedExpressionTree
    {
        private readonly IReadOnlyDictionary<string, SlottedExpressionTree> _caseSensitiveChildren;
        private readonly IReadOnlyDictionary<string, SlottedExpressionTree> _caseInsensitiveChildren;

        public bool HasChildren => _caseSensitiveChildren.Count > 0 || _caseInsensitiveChildren.Count > 0;
        public Option<int> Slot { get; }
        public bool Passthrough { get; }
        
        private SlottedExpressionTree(IReadOnlyDictionary<string, SlottedExpressionTree> caseSensitiveChildren, IReadOnlyDictionary<string, SlottedExpressionTree> caseInsensitiveChildren, Option<int> slot, bool passthrough)
        {
            _caseSensitiveChildren = caseSensitiveChildren;
            _caseInsensitiveChildren = caseInsensitiveChildren;
            
            Slot = slot;
            Passthrough = passthrough;
        }

        public (SlottedExpressionTree, SlottedExpressionTree) GetChildren(string name)
        {
            var a = _caseSensitiveChildren.TryGetValue(name, out var v1);
            var b = _caseInsensitiveChildren.TryGetValue(name, out var v2);

            return (a, b) switch
            {
                (true, true) => (v1, v2),
                (true, false) => (v1, null),
                (false, true) => (v2, null),
                (false, false) => (null, null),
            };
        }

        public class Builder
        {
            public BuilderNode Root { get; } = new();

            public SlottedExpressionTree Build() => Root.Build();

            public BuilderNode FindOrCreate(Expression.Qualified qualified) => FindOrCreate(Root, qualified, 0);
            private BuilderNode FindOrCreate(BuilderNode node, Expression.Qualified qualified, int identifierIndex)
            {
                while (true)
                {
                    var identifier = qualified.Identifiers[identifierIndex].Name;
                    var isCaseSensitive = qualified.Identifiers[identifierIndex].CaseSensitive;

                    if (identifier == "*")
                    {
                        if (identifierIndex + 1 != qualified.Identifiers.Count)
                        {
                            throw new InvalidOperationException("\"*\" identifier isn't last in the qualification chain");
                        }

                        return node;
                    }

                    BuilderNode child;
                    if (isCaseSensitive)
                    {
                        if (!node.CaseSensitiveChildren.TryGetValue(identifier, out child))
                        {
                            node.CaseSensitiveChildren[identifier] = child = new BuilderNode();
                        }
                    }
                    else
                    {
                        if (!node.CaseInsensitiveChildren.TryGetValue(identifier, out child))
                        {
                            node.CaseInsensitiveChildren[identifier] = child = new BuilderNode();
                        }
                    }
                
                    identifierIndex++;

                    if (identifierIndex == qualified.Identifiers.Count)
                    {
                        return child;
                    }

                    node = child;
                }
            }
        }

        public class BuilderNode
        {
            public Dictionary<string, BuilderNode> CaseSensitiveChildren { get; } = new();
            public Dictionary<string, BuilderNode> CaseInsensitiveChildren { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Option<int> Slot { get; set; } = Option.None;
            public bool Passthrough { get; set; } = true;

            public SlottedExpressionTree Build() => new SlottedExpressionTree(CaseSensitiveChildren.ToDictionary(x => x.Key, x => x.Value.Build()), CaseInsensitiveChildren.ToDictionary(x => x.Key, x => x.Value.Build(), StringComparer.OrdinalIgnoreCase), Slot, Passthrough);
        }
    }

    internal static class SlottedExpressionTreeExtensions
    {
        public static PooledList<SlottedExpressionTree> GetChildren(this PooledList<SlottedExpressionTree> nodes, string name)
        {
            var children = new PooledList<SlottedExpressionTree>(nodes.Count * 2);

            for (var index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                var (a, b) = node.GetChildren(name);

                if(a != null && b != null) children.Add(a, b);
                else if(a != null) children.Add(a);
                else if(b != null) children.Add(b);
            }

            return children;
        }
    }
}