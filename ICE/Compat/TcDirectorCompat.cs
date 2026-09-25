// =====================================================================================
// porting-note(api13): TC-only file. NOT from upstream ICE.
//
// A newer FFXIVClientStructs renamed UIState's `DirectorTodo` to `MassivePcContentTodo` and
// re-typed its `Director` field from `Director*` to `MassivePcContentDirector*`. At api13
// (CS 6966) the field is still `Director*`, so upstream's `director->MassivePcContentTodos`
// does not bind.
//
// The reinterpret below is NOT an invented offset: CS 6966 declares
// MassivePcContentDirector.Director at [FieldOffset(0x0)], so a MassivePcContentDirector*
// and its embedded Director* are the same address by construction.
//
// The payload shape already matches upstream exactly -- api13's
// MassivePcContentDirector.MassivePcContentTodos is Span<StdVector<MassivePcContentTodo>>,
// i.e. the same "group then index" access ICE performs.
// =====================================================================================

using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.MassivePcContent;
using FFXIVClientStructs.STD;
using System.Runtime.CompilerServices;

namespace ICE.Compat;

public static unsafe class TcDirectorCompat
{
    extension(ref Director d)
    {
        public Span<StdVector<MassivePcContentTodo>> MassivePcContentTodos
            => ((MassivePcContentDirector*)Unsafe.AsPointer(ref d))->MassivePcContentTodos;
    }
}
