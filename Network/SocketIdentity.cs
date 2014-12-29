using System;

namespace uNet2.Network
{
    public class SocketIdentity
    {
        public int Id { get; set; }

        public Guid Guid
        {
            get { return _guid; }
            set
            {
                _guid = value;
                _isSet = true;
            }
        }

        public bool IsSet
        {
            get { return _isSet; }
            set { _isSet = value; }
        }

        private Guid _guid;
        private bool _isSet;

        public SocketIdentity()
        {
            
        }

        public SocketIdentity(int id)
        {
            Id = id;
        }

        public SocketIdentity(int id, Guid guid) 
        {
            Id = id;
            Guid = guid;
            _isSet = true;
        }

        public override string ToString()
        {
            return Guid.ToString();
        }

        public static Guid GenerateGuid()
        {
            return Guid.NewGuid();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((SocketIdentity) obj);
        }

        protected bool Equals(SocketIdentity other)
        {
            return _guid.Equals(other._guid) && _isSet.Equals(other._isSet) && Id == other.Id;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _guid.GetHashCode();
                hashCode = (hashCode * 397) ^ _isSet.GetHashCode();
                hashCode = (hashCode * 397) ^ Id;
                return hashCode;
            }
        }
    }
}
