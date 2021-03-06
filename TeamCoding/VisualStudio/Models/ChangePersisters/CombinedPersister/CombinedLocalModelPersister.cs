﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamCoding.VisualStudio.Models.ChangePersisters.CombinedPersister
{
    public class CombinedLocalModelPersister : ILocalModelPerisister
    {
        private readonly ILocalModelPerisister[] LocalModelPersisters;
        public CombinedLocalModelPersister(params ILocalModelPerisister[] localModelPersisters)
        {
            LocalModelPersisters = localModelPersisters;
        }
        public async Task SendUpdateAsync()
        {
            await Task.WhenAll(LocalModelPersisters.Select(lmp => lmp.SendUpdateAsync()));
        }
        public void Dispose()
        {
            foreach(var localModelPersister in LocalModelPersisters)
            {
                localModelPersister.Dispose();
            }
        }
    }
}
