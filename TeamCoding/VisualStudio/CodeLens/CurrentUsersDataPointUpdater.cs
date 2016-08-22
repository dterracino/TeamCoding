﻿using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.CodeSense.Roslyn;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamCoding.Documents;
using TeamCoding.Extensions;

namespace TeamCoding.VisualStudio.CodeLens
{
    [Export(typeof(CurrentUsersDataPointUpdater))]
    public class CurrentUsersDataPointUpdater : IDisposable
    {
        private readonly List<CurrentUsersDataPointViewModel> DataPointModels = new List<CurrentUsersDataPointViewModel>();
        private Dictionary<int[], string> CaretMemberHashCodeToDataPointString = new Dictionary<int[], string>(new IntArrayEqualityComparer());
        private bool disposedValue = false; // To detect redundant calls
        public CurrentUsersDataPointUpdater(): base()
        {
            TeamCodingPackage.Current.RemoteModelChangeManager.RemoteModelReceived += RemoteModelChangeManager_RemoteModelReceived;
        }
        public void AddDataPointModel(CurrentUsersDataPointViewModel dataPointModel)
        {
            DataPointModels.Add(dataPointModel);
        }
        public void RemoveDataPointModel(CurrentUsersDataPointViewModel dataPointModel)
        {
            DataPointModels.Remove(dataPointModel);
        }
        private void RemoteModelChangeManager_RemoteModelReceived(object sender, EventArgs e)
        {
            var oldCaretMemberHashCodeToDataPointString = CaretMemberHashCodeToDataPointString;

            CaretMemberHashCodeToDataPointString = TeamCodingPackage.Current.RemoteModelChangeManager.GetOpenFiles()
                                              .Where(of => of.CaretPositionInfo != null)
                                              .Select(of => new
                                              {
                                                  CaretMemberHashCodes = of.CaretPositionInfo.SyntaxNodeIds,
                                                  of.IdeUserIdentity.DisplayName
                                              })
                                              .GroupBy(of => of.CaretMemberHashCodes, new IntArrayEqualityComparer())
                                              .ToDictionary(g => g.Key, g => "Current coders: " + string.Join(", ", g.Select(of => of.DisplayName).Distinct()));

            if (!oldCaretMemberHashCodeToDataPointString.DictionaryEqual(CaretMemberHashCodeToDataPointString))
            {
                foreach (var dataPointModel in DataPointModels)
                {
                    if (!dataPointModel.IsDisposed)
                    {
                        dataPointModel.RefreshModel();
                    }
                }
                DataPointModels.RemoveAll(dvm => dvm.IsDisposed);
            }
        }
        public Task<string> GetTextForDataPoint(ICodeElementDescriptor codeElementDescriptor)
        {
            foreach (var caret in CaretMemberHashCodeToDataPointString.Keys)
            {
                var node = codeElementDescriptor.SyntaxNode;

                // Find the first node that we start the node chain from
                var matchedNode = node.AncestorsAndSelf().FirstOrDefault(n => n.IsTrackedLeafNode());

                if (matchedNode?.GetValueBasedHashCode() == caret.Last())
                {
                    // Now walk up the tree, and up the method hashes ensuring we match all the way up
                    var nodeancestorhashes = matchedNode.AncestorsAndSelf().Select(a => a.GetValueBasedHashCode());
                    if (nodeancestorhashes.SequenceEqual(caret.Reverse()))
                    {
                        return Task.FromResult(CaretMemberHashCodeToDataPointString[caret]);
                    }
                }
            }
            return Task.FromResult<string>(null);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    TeamCodingPackage.Current.RemoteModelChangeManager.RemoteModelReceived -= RemoteModelChangeManager_RemoteModelReceived;
                }
                disposedValue = true;
            }
        }
        public void Dispose() { Dispose(true); }
    }
}
