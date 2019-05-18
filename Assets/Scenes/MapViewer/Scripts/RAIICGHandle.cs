using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

class RAIICGHandle : IDisposable
{
    private GCHandle m_handle;
    public RAIICGHandle(object value)
    {
        m_handle = GCHandle.Alloc(value,GCHandleType.Pinned);
    }

    public IntPtr Address
    {
        get
        {
            return m_handle.AddrOfPinnedObject();
        }
    }

    public void Dispose()
    {
        m_handle.Free();
    }
}
