using UnityEngine;

// 各ノーツ（人参）が持つべきデータ
public class NoteData : MonoBehaviour
{
    // このノーツをタップすべき目標時間（音楽の再生時間）
    [HideInInspector]
    public float targetTime;
    
    // ノーツが判定済みかどうか
    [HideInInspector]
    public bool isJudged = false;
    
    // ノーツの種類（大、中、小など）の識別子をここに追加しても良い
    // public string noteType;
}