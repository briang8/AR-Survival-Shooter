using System;
using System.Collections.Generic;

// Wrapper because JsonUtility cannot serialize a bare list
[Serializable]
public class SessionRecordList
{
    public List<SessionRecord> records = new List<SessionRecord>();
}