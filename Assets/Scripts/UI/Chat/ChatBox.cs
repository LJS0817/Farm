using TMPro;
using UnityEngine;

public class ChatBox : MonoBehaviour
{
    [SerializeField]
    TMP_Text _name;
    [SerializeField]
    TMP_Text _time;
    [SerializeField]
    TMP_Text _text;

    public void SetText(string str) { _text.SetText(str); }
    public string GetText() { return _text.text; }

    public void SetName(string str) { _name.SetText(str); }
    public string GetName() { return _name.text; }

    public void SetTime(string str) { _time.SetText(str); }
    public string GetTime() { return _time.text; }
}
