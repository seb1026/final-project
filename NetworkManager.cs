using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.IO;
using System;

public class QuitManager
{
    private Boolean savePoint;
    private Boolean quitRequest;

    public QuitManager()
    {
        savePoint = false;
        quitRequest = false;
    }

    public void Quit()
    {
        if (savePoint) quitRequest = true;
        else Application.Quit();
    }

    public void OnSaving()
    {
        savePoint = true;
    }

    public void OnSaved()
    {
        savePoint = false;
        if (quitRequest) Quit();
    }
}

public class NetworkManager : MonoBehaviourPunCallbacks
{
    string[] regions = new string[10] { "kr", "jp", "asia", "ru", "au", "cae", "us", "eu", "sa", "za" };
    int[] datas = new int[10] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
    string[] times = new string[10] { null, null, null, null, null, null, null, null, null, null };
    private int index = -1;
    public Text progress;
    QuitManager quitManager;

    private void Start()
    {
        quitManager = new QuitManager();
        ResetIndex();
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = regions[GetNextIndex()];
        PhotonNetwork.ConnectUsingSettings();
    }

    public void OnApplicationQuit()
    {
        quitManager.Quit();
    }

    public void OnQuit()
    {
        quitManager.Quit();
    }

    public override void OnConnectedToMaster()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            Writer(DateTime.Now.ToString("HHmmss"), PhotonNetwork.GetPing(), PhotonNetwork.CloudRegion);
            PhotonNetwork.Disconnect();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = regions[GetNextIndex()];
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    private void Writer(string time, int data, string region)
    {
        if (RegionEffectivenessCheck(region) && TimeEffectivenessCheck(time))
        {
            datas[GetNowIndex()] = data;
            times[GetNowIndex()] = time;
            if (GetNowIndex() == (regions.Length - 1))
            {
                WriteData();
                ResetAll();
            }
        }
        else
        {
            progress.text = "failed";
            ResetAll();
        }
    }

    private void WriteData()
    {
        quitManager.OnSaving();
        if (TotalEffectivenessCheck())
        {
            FileStream timeFile = new FileStream("RecordedData/" + getTitleTime(times[0].Substring(0, 2)) + ".txt", FileMode.Append, FileAccess.Write);
            StreamWriter writerTime = new StreamWriter(timeFile, System.Text.Encoding.Unicode);
            writerTime.WriteLine('$');
            for (int i = 0; i < regions.Length; ++i)
            {
                writerTime.Write(times[i]);
                writerTime.Write(",");
                writerTime.Write(datas[i]);
                writerTime.Write(",");
                writerTime.WriteLine(regions[i]);
                WriteTitle(i);
            }
            writerTime.Close();
            progress.text = times[0];
        }
        else progress.text = "failed";
        quitManager.OnSaved();
    }

    private void WriteTitle(int titleIndex)
    {
        FileStream titleFile = new FileStream("RecordedData/" + regions[titleIndex] + ".txt", FileMode.Append, FileAccess.Write);
        StreamWriter writerTitle= new StreamWriter(titleFile, System.Text.Encoding.Unicode);
        writerTitle.Write(times[titleIndex]);
        writerTitle.Write(",");
        writerTitle.WriteLine(datas[titleIndex]);
        writerTitle.Close();
    }

    private string getTitleTime(string time)
    {
        int t = Int32.Parse(time);
        string titleOfTime;
        switch (t / 6)
        {
            case 0:
                titleOfTime = "midnight";
                break;
            case 1:
                titleOfTime = "morning";
                break;
            case 2:
                titleOfTime = "afternoon";
                break;
            case 3:
                titleOfTime = "evening";
                break;
            default:
                titleOfTime = "errors";
                break;
        }
        return titleOfTime;
    }

    private bool TotalEffectivenessCheck()
    {
        for (int i = 0; i < regions.Length; ++i)
        {
            if ((datas[i] == -1) || (string.IsNullOrEmpty(times[i]))) return false;
        }
        return true;
    }

    private bool RegionEffectivenessCheck(string region)
    {
        if (regions[GetNowIndex()].Equals(region)) return true;
        else return false;
    }

    private bool TimeEffectivenessCheck(string time)
    {
        if (string.IsNullOrEmpty(time)) return false;
        else
        {
            if (GetNowIndex() == 0) return true;
            else if (!string.IsNullOrEmpty(times[GetBeforeIndex()]))
            {
                if ((Int32.Parse(time) - Int32.Parse(times[GetBeforeIndex()])) < 500) return true;
                else return false;
            }
            else return false;
        }
    }

    private void ResetAll()
    {
        ResetIndex();
        ResetDatas();
        ResetTimes();
    }

    private void ResetDatas()
    {
        for(int i = 0; i < datas.Length; ++i)
        {
            datas[i] = -1;
        }
    }

    private void ResetTimes()
    {
        for (int i = 0; i < times.Length; ++i)
        {
            times[i] = null;
        }
    }

    private void ResetIndex()
    {
        index = -1;
    }

    private int GetNowIndex()
    {
        return index;
    }

    private int GetNextIndex()
    {
        if (index > (regions.Length - 2)) index = -1;
        ++index;
        return index;
    }

    private int GetBeforeIndex()
    {
        if (GetNowIndex() == 0) return 0;
        else return GetNowIndex() - 1;
    }
}
