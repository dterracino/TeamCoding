﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamCoding.Options
{
    internal class Property<TProperty> where TProperty : IEquatable<TProperty>
    {
        private readonly object Owner;
        public Property(object owner) { Owner = owner; }
        private TProperty _Value;
        public TProperty Value
        {
            get { return _Value; }
            set
            {
                if (!EqualityComparer<TProperty>.Default.Equals(_Value, value))
                {
                    _Value = value;
                    Changed?.Invoke(Owner, EventArgs.Empty);
                }
            }
        }
        public event EventHandler Changed;
    }
}
