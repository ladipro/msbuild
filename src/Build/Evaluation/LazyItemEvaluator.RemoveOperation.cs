// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Construction;
using Microsoft.Build.Shared;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.Build.Evaluation
{
    internal partial class LazyItemEvaluator<P, I, M, D>
    {
        class RemoveOperation : LazyItemOperation
        {
            readonly MatchOnMetadataState _matchOnMetadataState;

            public RemoveOperation(RemoveOperationBuilder builder, LazyItemEvaluator<P, I, M, D> lazyEvaluator)
                : base(builder, lazyEvaluator)
            {
                _matchOnMetadataState = new MatchOnMetadataState(builder.MatchOnMetadata.ToImmutable(), builder.MatchOnMetadataOptions);
            }

            /// <summary>
            /// Apply the Remove operation.
            /// </summary>
            /// <remarks>
            /// This operation is mostly implemented in terms of the default <see cref="LazyItemOperation.ApplyImpl(ImmutableList{ItemData}.Builder, ImmutableHashSet{string})"/>.
            /// This override exists to apply the removing-everything short-circuit.
            /// </remarks>
            protected override void ApplyImpl(ImmutableList<ItemData>.Builder listBuilder, ImmutableHashSet<string> globsToIgnore)
            {
                var matchOnMetadataValid = !_matchOnMetadataState.IsEmpty && _itemSpec.Fragments.Count == 1
                    && _itemSpec.Fragments.First() is ItemSpec<ProjectProperty, ProjectItem>.ItemExpressionFragment;
                ProjectFileErrorUtilities.VerifyThrowInvalidProjectFile(
                    _matchOnMetadataState.IsEmpty || (matchOnMetadataValid && _matchOnMetadataState.Count == 1),
                    new BuildEventFileInfo(string.Empty),
                    "OM_MatchOnMetadataIsRestrictedToOnlyOneReferencedItem");

                if (_matchOnMetadataState.IsEmpty && ItemspecContainsASingleBareItemReference(_itemSpec, _itemElement.ItemType) && _conditionResult)
                {
                    // Perf optimization: If the Remove operation references itself (e.g. <I Remove="@(I)"/>)
                    // then all items are removed and matching is not necessary
                    listBuilder.Clear();
                    return;
                }

                base.ApplyImpl(listBuilder, globsToIgnore);
            }

            // todo Perf: do not match against the globs: https://github.com/Microsoft/msbuild/issues/2329
            protected override ImmutableList<I> SelectItems(ImmutableList<ItemData>.Builder listBuilder, ImmutableHashSet<string> globsToIgnore)
            {
                var items = ImmutableHashSet.CreateBuilder<I>();
                foreach (ItemData item in listBuilder)
                {
                    if (_matchOnMetadataState.IsEmpty ? _itemSpec.MatchesItem(item.Item) : _itemSpec.MatchesItemOnMetadata(item.Item, _matchOnMetadataState))
                        items.Add(item.Item);
                }

                return items.ToImmutableList();
            }

            protected override void SaveItems(ImmutableList<I> items, ImmutableList<ItemData>.Builder listBuilder)
            {
                if (!_conditionResult)
                {
                    return;
                }

                listBuilder.RemoveAll(itemData => items.Contains(itemData.Item));
            }

            public ImmutableHashSet<string>.Builder GetRemovedGlobs()
            {
                var builder = ImmutableHashSet.CreateBuilder<string>();

                if (!_conditionResult)
                {
                    return builder;
                }

                var globs = _itemSpec.Fragments.OfType<GlobFragment>().Select(g => g.TextFragment);

                builder.UnionWith(globs);

                return builder;
            }
        }

        class RemoveOperationBuilder : OperationBuilder
        {
            public ImmutableList<string>.Builder MatchOnMetadata { get; } = ImmutableList.CreateBuilder<string>();

            public MatchOnMetadataOptions MatchOnMetadataOptions { get; set; }

            public RemoveOperationBuilder(ProjectItemElement itemElement, bool conditionResult) : base(itemElement, conditionResult)
            {
            }
        }
    }

    public enum MatchOnMetadataOptions
    {
        CaseSensitive,
        CaseInsensitive,
        PathLike
    }

    public static class MatchOnMetadataConstants {
        public const MatchOnMetadataOptions MatchOnMetadataOptionsDefaultValue = MatchOnMetadataOptions.CaseSensitive;
    }

    internal sealed class MatchOnMetadataState
    {
        public readonly MatchOnMetadataOptions Options;
        public readonly IEnumerable<string> Metadata;

        // TODO: Add cache

        public bool IsEmpty => Count == 0;

        public int Count
        {
            get
            {
                if (Metadata is ImmutableList<string> list)
                {
                    return list.Count;
                }
                return Metadata.Count();
            }
        }

        public MatchOnMetadataState(IEnumerable<string> metadata, MatchOnMetadataOptions options)
        {
            Metadata = metadata;
            Options = options;

        }
    }
}
