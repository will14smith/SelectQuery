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
        public IReadOnlyDictionary<string, SlottedExpressionTree> Children { get; }
        public Option<int> Slot { get; }
        public bool Passthrough { get; }

        private SlottedExpressionTree(IReadOnlyDictionary<string, SlottedExpressionTree> children, Option<int> slot, bool passthrough)
        {
            Children = children;
            Slot = slot;
            Passthrough = passthrough;
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

                    if (identifier == "*")
                    {
                        if (identifierIndex + 1 != qualified.Identifiers.Count)
                        {
                            throw new InvalidOperationException("\"*\" identifier isn't last in the qualification chain");
                        }

                        return node;
                    }
                
                    if (!node.Children.TryGetValue(identifier, out var child))
                    {
                        node.Children[identifier] = child = new BuilderNode();
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
            public Dictionary<string, BuilderNode> Children { get; } = new();
            public Option<int> Slot { get; set; } = Option.None;
            public bool Passthrough { get; set; } = true;

            public SlottedExpressionTree Build() => new SlottedExpressionTree(Children.ToDictionary(x => x.Key, x => x.Value.Build()), Slot, Passthrough);
        }
    }
}