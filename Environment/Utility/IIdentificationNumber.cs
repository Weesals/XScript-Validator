using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Environment.Entities;

namespace RTS4.Environment.Utility {
    public interface IIdentificationNumber {

        void SetId(int id);
        int Id { get; }

    }
}
