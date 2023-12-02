namespace Tefin.ViewModels.Types.TypeEditors;

public interface ITypeEditor : IDisposable {
    bool AcceptsNull { get; }
    string FormattedValue { get; }
    bool IsEditing { get; set; }
    bool IsNull { get; set; }
    TypeBaseNode Node { get; }
}

public interface ITypeEditor<T> : ITypeEditor {
    T? TempValue { get; set; }

    void CommitEdit();
}