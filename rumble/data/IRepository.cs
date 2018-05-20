using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rumble {
    public interface IRepository {
        void Init();
        T Load<T>(string id);

        List<T> Load<T>();

        Dictionary<K, T> Load<K, T>();

        void Save<T>(string id, T data);

        void Save<T>(List<T> data);

        void Save<K, T>(Dictionary<K, T> data);

        bool Exists<T>(string id);
    }
}
