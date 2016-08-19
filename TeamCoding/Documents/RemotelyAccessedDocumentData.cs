﻿using System;
using TeamCoding.IdentityManagement;

namespace TeamCoding.Documents
{
    /// <summary>
    /// Contains data about a document belonging to source control that's open by a user
    /// </summary>
    public class RemotelyAccessedDocumentData : IEquatable<RemotelyAccessedDocumentData>
    {
        public string Repository { get; set; }
        public string RepositoryBranch { get; set; }
        public string RelativePath { get; set; }
        public UserIdentity IdeUserIdentity { get; set; }
        public bool BeingEdited { get; set; }
        public bool HasFocus { get; set; }
        public int[] CaretMemberHashCode { get; set; }
        public override int GetHashCode()
        {
            return Repository.GetHashCode() ^ RepositoryBranch.GetHashCode() ^ IdeUserIdentity.Id.GetHashCode() ^ BeingEdited.GetHashCode() ^ HasFocus.GetHashCode() ^ (CaretMemberHashCode?.GetHashCode() ?? 0);
        }
        public bool Equals(RemotelyAccessedDocumentData other)
        {
            if (other == null)
                return false;

            return Repository == other.Repository &&
                   RepositoryBranch == other.RepositoryBranch &&
                   RelativePath == other.RelativePath &&
                   IdeUserIdentity.Id == IdeUserIdentity.Id &&
                   BeingEdited == other.BeingEdited &&
                   HasFocus == other.HasFocus &&
                   CaretMemberHashCode == other.CaretMemberHashCode;
        }
        public override bool Equals(object obj)
        {
            var typedObj = obj as RemotelyAccessedDocumentData;
            return Equals(typedObj);
        }
    }
}