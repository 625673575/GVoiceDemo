using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using gcloud_voice;
using System;
using System.IO;

public class GVoiceManager : GoSingleton2<GVoiceManager>
{
    private string m_authkey; /*this key should get from your game svr*/
    private byte[] m_ShareFileID = null; /*when send record file save in svr, we will return a fileid in OnSendFileComplete callback function, you can save it ,and download  record by this fileid*/
    private IGCloudVoice m_voiceengine;  //engine have init int mainscene start function
    private string m_roomName;
    private int m_member_Id;
    private GCloudVoiceRole m_role;
    private string RecordPath;
    private string DownloadPath;
    private string m_fileid = "";
    public bool hasApplyMessageKey = false;
    private string s_strLog;
    private static bool bIsStart = false;
    private string recentPlayedFile;
    public IGCloudVoice.JoinRoomCompleteHandler OnJoinRoomComplete;
    public IGCloudVoice.MemberVoiceHandler OnMemberVoice;
    public IGCloudVoice.QuitRoomCompleteHandler OnQuitRoomComplete;
    public IGCloudVoice.StatusUpdateHandler OnStatusUpdate;
    public IGCloudVoice.StreamSpeechToTextHandler OnStreamSpeechToText;

    public IGCloudVoice.ApplyMessageKeyCompleteHandler OnApplyMessageKeyComplete;
    public IGCloudVoice.UploadReccordFileCompleteHandler OnUploadReccordFileComplete;
    public IGCloudVoice.DownloadRecordFileCompleteHandler OnDownloadRecordFileComplete;
    public IGCloudVoice.PlayRecordFilCompleteHandler OnPlayRecordFilComplete;
    public IGCloudVoice.SpeechToTextHandler OnSpeechToText;
    /// <summary>
    /// 是否在实时语音中
    /// </summary>
    public bool bIsInRealTime;
    /// <summary>
    /// 是否是在播放语音
    /// </summary>
    public bool bIsPlayingVoice;

    protected override void _init()
    {
        base._init();
    }

    protected override void _release()
    {
        if (bIsInRealTime)
            QuitNationRoom(m_roomName);
        base._release();
    }

    void Update()
    {
        if (m_voiceengine != null)
        {
            m_voiceengine.Poll();
        }
    }

    public void OnApplicationPause(bool pause)
    {
        if (m_voiceengine == null)
        {
            return;
        }
        if (pause)
            m_voiceengine.Pause();
        else
            m_voiceengine.Resume();
    }
    public void InitGVoiceEngine()
    {
        if (m_voiceengine == null)
        {
            m_voiceengine = GCloudVoice.GetEngine();
            System.TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            string strTime = System.Convert.ToInt64(ts.TotalSeconds).ToString();
            //m_voiceengine.SetAppInfo("932849489","d94749efe9fce61333121de84123ef9b",strTime);
            m_voiceengine.SetAppInfo("1384863990", "19a859ab145551f926762282843cbf6f", strTime);
            m_voiceengine.Init();
            InitGVoiceCallback();
            RecordPath = Application.persistentDataPath + "/record.dat";
            DownloadPath = Application.persistentDataPath + "/voice/";
            if (!Directory.Exists(DownloadPath))
            {
                Directory.CreateDirectory(DownloadPath);
            }

            DeleteObsoleteDownloadFile(TimeSpan.FromHours(24.0));
            Debug.Log("Init GVoiceEngine");
        }
    }
    public void InitMessageEngine()
    {
        if (!hasApplyMessageKey)
        {
            m_voiceengine.ApplyMessageKey(15000);
        }
        Debug.Log("GVoiceEngine ApplyMessageKey");
    }
    private void OnDestroy()
    {
        DestroyGVoiceCallback();
    }
    public void SetMode(GCloudVoiceMode mode)
    {
        m_voiceengine.SetMode(mode);
    }
    string GetDownloadFilePath(string fileId)
    {
        return DownloadPath + fileId + ".dat";
    }
    void DeleteObsoleteDownloadFile(TimeSpan span)
    {
        int count = 0;
        foreach (string f in Directory.GetFiles(DownloadPath))
        {
            FileInfo info = new FileInfo(f);
            if (DateTime.Now > info.CreationTime + span)
            {
                info.Delete();
                count++;
            }
        }
        if (count > 0)
            Debug.Log("删除了" + count + "个文件");
    }
    void InitGVoiceCallback()
    {
        if (!bIsStart)
        {
            bIsStart = true;
            m_voiceengine.OnApplyMessageKeyComplete += DefaultOnApplyMessageKeyComplete;
            m_voiceengine.OnDownloadRecordFileComplete += DefaultOnDownloadRecordFileComplete;
            m_voiceengine.OnJoinRoomComplete += DefaultOnJoinRoomComplete;
            m_voiceengine.OnMemberVoice += DefaultOnMemberVoice;

            m_voiceengine.OnPlayRecordFilComplete += DefaultOnPlayRecordFilComplete;
            m_voiceengine.OnQuitRoomComplete += DefaultOnQuitRoomComplete;
            m_voiceengine.OnSpeechToText += DefaultOnSpeechToText;
            m_voiceengine.OnStatusUpdate += DefaultOnStatusUpdate;
            m_voiceengine.OnStreamSpeechToText += DefaultOnStreamSpeechToText;
            m_voiceengine.OnUploadReccordFileComplete += DefaultOnUploadReccordFileComplete;
        }
    }

    private void DefaultOnApplyMessageKeyComplete(IGCloudVoice.GCloudVoiceCompleteCode code)
    {
        if (code == IGCloudVoice.GCloudVoiceCompleteCode.GV_ON_MESSAGE_KEY_APPLIED_SUCC)
        {
            Debug.Log("Apply key succ");
            hasApplyMessageKey = true;
            if (OnApplyMessageKeyComplete != null)
            {
                OnApplyMessageKeyComplete(code);
                OnApplyMessageKeyComplete = null;
            }
        }
        else
        {
            Debug.LogError("Apply key error" + code);
        }
    }
    private void DefaultOnUploadReccordFileComplete(IGCloudVoice.GCloudVoiceCompleteCode code, string filepath, string fileid)
    {
        if (code == IGCloudVoice.GCloudVoiceCompleteCode.GV_ON_UPLOAD_RECORD_DONE)
        {
            //将fileid传输给服务器
            m_fileid = fileid;
            if (OnUploadReccordFileComplete != null)
            {
                OnUploadReccordFileComplete(code, filepath, fileid);
                OnUploadReccordFileComplete = null;
            }
            Debug.Log("OnUploadReccordFileComplete succ, filepath:" + filepath + " fileid len=" + fileid.Length + " fileid:" + fileid + " fileid len=" + fileid.Length);
        }
        else
        {
            Debug.LogError("OnUploadReccordFileComplete error " + code);
        }

        if (bIsInRealTime)
        {
            SetMode(GCloudVoiceMode.RealTime);
        }
    }
    private void DefaultOnDownloadRecordFileComplete(IGCloudVoice.GCloudVoiceCompleteCode code, string filepath, string fileid)
    {
        if (code == IGCloudVoice.GCloudVoiceCompleteCode.GV_ON_DOWNLOAD_RECORD_DONE)
        {
            Debug.Log("OnDownloadRecordFileComplete succ, filepath:" + filepath + " fileid:" + fileid+ bIsPlayingVoice);
            //m_voiceengine.PlayRecordedFile(filepath);
            if (OnDownloadRecordFileComplete != null)
            {
                OnDownloadRecordFileComplete(code, filepath, fileid);
                OnDownloadRecordFileComplete = null;
            }
        }
        else
        {
            Debug.LogError("OnDownloadRecordFileComplete error");
        }
    }

    private void DefaultOnJoinRoomComplete(IGCloudVoice.GCloudVoiceCompleteCode code, string roomName, int memberID)
    {
        if (code == IGCloudVoice.GCloudVoiceCompleteCode.GV_ON_JOINROOM_SUCC)
        {
            m_roomName = roomName;
            m_member_Id = memberID;
            Debug.Log("OnJoinRoomComplete ret=" + code + " roomName:" + roomName + " memberID:" + memberID);
            if (OnJoinRoomComplete != null)
            {
                OnJoinRoomComplete(code, roomName, memberID);
                OnJoinRoomComplete = null;
            }
            OpenSpeaker(true);
        }
    }
    private void DefaultOnQuitRoomComplete(IGCloudVoice.GCloudVoiceCompleteCode code, string roomName, int memberID)
    {
        if (code == IGCloudVoice.GCloudVoiceCompleteCode.GV_ON_QUITROOM_SUCC)
        {
            m_roomName = roomName;
            if (OnQuitRoomComplete != null)
            {
                OnQuitRoomComplete(code, roomName, memberID);
            }
            Debug.Log("OnQuitRoomComplete ret=" + code + " roomName:" + roomName + " memberID:" + memberID);
        }
    }

    private void DefaultOnMemberVoice(int[] members, int count)
    {
        string s_logstr = null;
        for (int i = 0; i < count && (i + 1) < members.Length; ++i)
        {
            s_logstr += "\r\nmemberid:" + members[i] + "  state:" + members[i + 1];
        }
        if (OnMemberVoice != null)
        {
            OnMemberVoice(members, count);
            OnMemberVoice = null;
        }
        //UIManager.m_Instance.UpdateMemberState(members, length, usingCount);
    }

    private void DefaultOnPlayRecordFilComplete(IGCloudVoice.GCloudVoiceCompleteCode code, string filepath)
    {
        if (code == IGCloudVoice.GCloudVoiceCompleteCode.GV_ON_PLAYFILE_DONE)
        {
            Debug.Log("OnPlayRecordFilComplete succ, filepath:" + filepath);
            if (OnPlayRecordFilComplete != null)
            {
                OnPlayRecordFilComplete(code, filepath);
                OnPlayRecordFilComplete = null;
            }
        }
        else
        {
            Debug.LogError("OnPlayRecordFilComplete error");
        }
        bIsPlayingVoice = false;
        if (bIsInRealTime)
            SetMode(GCloudVoiceMode.RealTime);
    }

    private void DefaultOnSpeechToText(IGCloudVoice.GCloudVoiceCompleteCode code, string fileID, string result)
    {
        if (code == IGCloudVoice.GCloudVoiceCompleteCode.GV_ON_STT_SUCC)
        {
            Debug.Log("OnSpeechToText succ, result:" + result);
            if (OnSpeechToText != null)
            {
                OnSpeechToText(code, fileID, result);
                OnSpeechToText = null;
            }
        }
        else
        {
            Debug.LogError("OnSpeechToText error," + code);
        }
    }

    private void DefaultOnStatusUpdate(IGCloudVoice.GCloudVoiceCompleteCode code, string roomName, int memberID)
    {
        Debug.Log(code + "  " + roomName + ":" + memberID);

    }

    private void DefaultOnStreamSpeechToText(IGCloudVoice.GCloudVoiceCompleteCode code, int error, string result)
    {
        Debug.Log(code + " error: " + error + "  result:" + result);
    }
    void DestroyGVoiceCallback()
    {
        if (bIsStart)
        {
            bIsStart = false;
            m_voiceengine.OnApplyMessageKeyComplete -= OnApplyMessageKeyComplete;
            m_voiceengine.OnUploadReccordFileComplete -= OnUploadReccordFileComplete;
            m_voiceengine.OnDownloadRecordFileComplete -= OnDownloadRecordFileComplete;
            m_voiceengine.OnJoinRoomComplete -= OnJoinRoomComplete;
            m_voiceengine.OnMemberVoice -= OnMemberVoice;

            m_voiceengine.OnPlayRecordFilComplete -= OnPlayRecordFilComplete;
            m_voiceengine.OnQuitRoomComplete -= OnQuitRoomComplete;
            m_voiceengine.OnSpeechToText -= OnSpeechToText;
            m_voiceengine.OnStatusUpdate -= OnStatusUpdate;
            m_voiceengine.OnStreamSpeechToText -= OnStreamSpeechToText;
            m_voiceengine.OnUploadReccordFileComplete -= OnUploadReccordFileComplete;
        }
        OnApplyMessageKeyComplete += null;
        OnUploadReccordFileComplete += null;
        OnDownloadRecordFileComplete += null;
        OnJoinRoomComplete += null;
        OnQuitRoomComplete += null;
        OnMemberVoice += null;
        OnPlayRecordFilComplete += null;
        OnSpeechToText += null;
        OnStatusUpdate += null;
        OnStreamSpeechToText += null;
    }
    public void JoinTeamRoom(string roomName)
    {
        m_voiceengine.JoinTeamRoom(roomName, 10000);
        bIsInRealTime = true;
    }
    public void JoinNationalRoom(string roomName, GCloudVoiceRole role)
    {
        m_roomName = roomName;
        m_voiceengine.JoinNationalRoom(roomName, role, 10000);
        m_role = role;
        bIsInRealTime = true;
    }
    public void QuitNationRoom(string roomName)
    {
        m_voiceengine.QuitRoom(roomName, 10000);
        bIsInRealTime = false;
    }
    public void SwitchNationalRole(GCloudVoiceRole new_role)
    {
        if (string.IsNullOrEmpty(m_roomName))
        {
            Debug.LogError("请输入有效的房间号");
        }
        m_role = new_role;
        m_voiceengine.QuitRoom(m_roomName, 20000);
        m_voiceengine.OnQuitRoomComplete += AfterQuitSwitchRole;
    }
    /// <summary>
    /// 在房间后以一个新的角色加入到国战房间
    /// </summary>
    /// <param name="code"></param>
    /// <param name="roomName"></param>
    /// <param name="memberID"></param>
    private void AfterQuitSwitchRole(IGCloudVoice.GCloudVoiceCompleteCode code, string roomName, int memberID)
    {
        JoinNationalRoom(m_roomName, m_role);
        m_voiceengine.OnQuitRoomComplete -= AfterQuitSwitchRole;
        m_voiceengine.OnJoinRoomComplete += SwitchRoleSucc;
    }

    private void SwitchRoleSucc(IGCloudVoice.GCloudVoiceCompleteCode code, string roomName, int memberID)
    {
        Debug.Log("角色切换成功");
        m_voiceengine.OnJoinRoomComplete -= SwitchRoleSucc;
    }

    public void StartRecording()
    {
        if (bIsInRealTime)
        {
            SetMode(GCloudVoiceMode.Translation);
        }
        Debug.Log("录音位置" + RecordPath);
        m_voiceengine.StartRecording(RecordPath);
    }
    public void StopRecording()
    {
        m_voiceengine.StopRecording();
    }
    void PlayDownloadRecordFile(IGCloudVoice.GCloudVoiceCompleteCode code, string filepath, string fileid)
    {
        if (code == IGCloudVoice.GCloudVoiceCompleteCode.GV_ON_DOWNLOAD_RECORD_DONE)
        {
            m_voiceengine.PlayRecordedFile(filepath);
            if(_onStartPlay!=null)
            {
                _onStartPlay();
            }
            bIsPlayingVoice = true;
        }
        m_voiceengine.OnDownloadRecordFileComplete -= PlayDownloadRecordFile;
    }
    private System.Action _onStartPlay;
    public void PlayFileID(string fileId,System.Action onStartPlay, System.Action onPlayFinish)
    {
        SetMode(GCloudVoiceMode.Translation);
        if (bIsPlayingVoice)
        {
            bIsPlayingVoice = false;
            if (OnPlayRecordFilComplete != null)
            {
                OnPlayRecordFilComplete(IGCloudVoice.GCloudVoiceCompleteCode.GV_ON_DOWNLOAD_RECORD_DONE, null);
                OnPlayRecordFilComplete = null;
            }
        }

        recentPlayedFile = GetDownloadFilePath(fileId);
        Debug.Log("下载音频位置" + recentPlayedFile);
        if (File.Exists(recentPlayedFile))
        {
            bIsPlayingVoice = true;
            m_voiceengine.PlayRecordedFile(recentPlayedFile);
            if (onStartPlay != null)
                onStartPlay();
        }
        else
        {
            m_voiceengine.DownloadRecordedFile(fileId, GetDownloadFilePath(fileId), 10000);
            _onStartPlay = onStartPlay;
            m_voiceengine.OnDownloadRecordFileComplete += PlayDownloadRecordFile;//play when finish download
        }
        if (onPlayFinish != null)
            OnPlayRecordFilComplete = (code, file) => { onPlayFinish(); };
        
    }
    public void UploadFile()
    {
        m_voiceengine.UploadRecordedFile(RecordPath, 20000);
    }
    /// <summary>
    /// 开启接收语音
    /// </summary>
    public void OpenSpeaker(bool enable)
    {
        if (enable)
            m_voiceengine.OpenSpeaker();
        else
            m_voiceengine.CloseSpeaker();
    }
    public bool IsMicOpen
    {
        get;private set;
    }
    /// <summary>
    /// 是否开启麦克风，用于事实语音
    /// </summary>
    /// <param name="enable"></param>
    public void OpenMic(bool enable)
    {
        IsMicOpen = enable;
        if (enable)
            m_voiceengine.OpenMic();
        else
            m_voiceengine.CloseMic();
    }

    public void ForbidMember(int memberId, bool enable)
    {
        m_voiceengine.ForbidMemberVoice(memberId, enable);
    }
    public int GetRecordDuration()
    {
        int[] bytes = new int[1];
        bytes[0] = 0;
        float[] seconds = new float[1];
        seconds[0] = 0;
        m_voiceengine.GetFileParam(RecordPath, bytes, seconds);
        return Mathf.CeilToInt(seconds[0]);
    }
    System.Action<string> onGetSpeechToText;
    public void SpeechToText(string fileID, System.Action<string> onGetResult)
    {
        m_voiceengine.SpeechToText(fileID);
        m_voiceengine.OnSpeechToText += GetSpeechToTextResult;
        onGetSpeechToText = onGetResult;
    }
    private void GetSpeechToTextResult(IGCloudVoice.GCloudVoiceCompleteCode code, string fileID, string result)
    {
        if (code == IGCloudVoice.GCloudVoiceCompleteCode.GV_ON_STT_SUCC)
        {
            if (onGetSpeechToText != null)
            {
                onGetSpeechToText(result);
            }
        }

        onGetSpeechToText = null;
        m_voiceengine.OnSpeechToText -= GetSpeechToTextResult;
    }
}