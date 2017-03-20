﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamCoding.Documents;
using TeamCoding.Documents.SourceControlRepositories;
using TeamCoding.Extensions;
using TeamCoding.Interfaces.Documents;
using TeamCoding.Interfaces.Extensions;

namespace TeamCoding.Documents
{
    public class CaretInfoProvider : ICaretInfoProvider
    {
        private readonly ISourceControlRepository SourceControlRepository = TeamCodingProjectTypeProvider.Get<ITeamCodingPackageProvider>().SourceControlRepository;
        public async Task<DocumentRepoMetaData.CaretInfo> GetCaretInfoAsync(SnapshotPoint snapshotPoint)
        {
            var document = snapshotPoint.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return GetTextCaretInfo(snapshotPoint);
            }

            var syntaxRoot = await document.GetSyntaxRootAsync();
            if (syntaxRoot == null)
            {
                return null;
            }

            return GetRoslynCaretInfo(syntaxRoot, snapshotPoint);
        }
        private DocumentRepoMetaData.CaretInfo GetRoslynCaretInfo(SyntaxNode syntaxRoot, SnapshotPoint snapshotPoint)
        {
            var caretToken = syntaxRoot.FindToken(snapshotPoint);
            int[] memberHashCodes = null;
            IEnumerable<SyntaxNode> memberNodes = null;

            var desiredLeafNode = caretToken.Parent.AncestorsAndSelf().FirstOrDefault(n => n.IsUniquelyIdentifiedNode());

            switch (caretToken.Language)
            {
                case "C#":
                case "Visual Basic":
                    memberNodes = caretToken.Parent.AncestorsAndSelf().Reverse().TakeWhileInclusive(n => n != desiredLeafNode).ToArray();
                    memberHashCodes = memberNodes.Select(n => n.GetValueBasedHashCode()).ToArray();
                    break;
                default:
                    TeamCodingProjectTypeProvider.Get<ITeamCodingPackageProvider>().Logger.WriteInformation($"Document with unsupported language found: {caretToken.Language}"); return null;
            }

            var lastNode = memberNodes.Last();

            var caretLine = snapshotPoint.GetContainingLine();
            var lastNodeLine = snapshotPoint.Snapshot.GetLineFromPosition(lastNode.Span.Start);

            int leafMemberLineOffset = caretLine.LineNumber - lastNodeLine.LineNumber;

            return new DocumentRepoMetaData.CaretInfo()
            {
                SyntaxNodeIds = memberHashCodes,
                LeafMemberLineOffset = leafMemberLineOffset,
                LeafMemberCaretOffset = snapshotPoint.Position - caretLine.Start
            };
        }
        private DocumentRepoMetaData.CaretInfo GetTextCaretInfo(SnapshotPoint snapshotPoint)
        {
            ITextSnapshotLine textSnapshotLine = snapshotPoint.GetContainingLine();
            var caretColumn = snapshotPoint.Position - textSnapshotLine.Start;

            var textSnapshotLineNumber = textSnapshotLine.LineNumber;

            var filePath = snapshotPoint.Snapshot.TextBuffer.GetTextDocumentFilePath();

            if (filePath != null && SourceControlRepository.GetRepoDocInfo(filePath) != null)
            {
                var remoteFileText = SourceControlRepository.GetRemoteFileLines(filePath);

                if (remoteFileText != null)
                {
                    var localFileText = snapshotPoint.Snapshot.GetText().Split(new[] { "\r\n" }, StringSplitOptions.None);
                    
                    textSnapshotLineNumber = LineNumberTranslator.GetLineNumber(localFileText, remoteFileText, textSnapshotLineNumber, FileNumberBasis.Server);
                }
            }

            return new DocumentRepoMetaData.CaretInfo()
            {
                SyntaxNodeIds = new int[1], // Dummy value
                LeafMemberLineOffset = textSnapshotLineNumber,
                LeafMemberCaretOffset = caretColumn
            };
        }
    }
}
