﻿
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class BurglarAlarmScript : MonoBehaviour {

    public KMAudio Audio;
    public KMSelectable[] Buttons;
    public KMBombModule Module;
    public TextMesh DisplayText;
    public KMBombInfo Info;
    public KMSelectable ActivateButton;
    public KMSelectable SubmitButton;

    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;
    private bool isSolved = false;

    private int[] moduleNumber;
    private IList<INumberHandler> numberHandlers;
    private int[] answers;
    private bool activated;

    private System.Random rnd = new System.Random(DateTimeOffset.UtcNow.GetHashCode());

    private Regex TwitchPlayRegex = new Regex(@"^submit +(\d{8})$");

    private KMAudio.KMAudioRef activationSound;

    private int noPressed;
    // Use this for initialization
    void Start()
    {      
        _moduleId = _moduleIdCounter++;
        this.activated = false;
        this.moduleNumber = new int[8];
        this.answers = new int[8];
        this.numberHandlers = new List<INumberHandler>();

        for (int i = 0; i < this.moduleNumber.Length; ++i)
        {
            this.moduleNumber[i] = this.rnd.Next(0, 10);
        }

        var burglarAlarmHelper = new BurglarAlarmHelper(this.moduleNumber, this.Info);

        numberHandlers.Add(new NumberHandlerPos1(burglarAlarmHelper));
        numberHandlers.Add(new NumberHandlerPos2(burglarAlarmHelper));
        numberHandlers.Add(new NumberHandlerPos3(burglarAlarmHelper));
        numberHandlers.Add(new NumberHandlerPos4(burglarAlarmHelper));
        numberHandlers.Add(new NumberHandlerPos5(burglarAlarmHelper));
        numberHandlers.Add(new NumberHandlerPos6(burglarAlarmHelper));
        numberHandlers.Add(new NumberHandlerPos7(burglarAlarmHelper));
        numberHandlers.Add(new NumberHandlerPos8(burglarAlarmHelper));

        Debug.LogFormat("[Burglar Alarm #{0}] Module number is: {1}.", this._moduleId, string.Join(",",  this.moduleNumber.Select(x => x.ToString()).ToArray()));

        this.DisplayText.text = burglarAlarmHelper.ToStringNumber;
        for (int i = 0; i < this.Buttons.Count(); ++i)
        {
            var myIndex = i;
            this.Buttons[i].OnInteract += delegate ()
            {
                Audio.PlaySoundAtTransform("Button sound", this.Buttons[myIndex].transform);
                this.Buttons[myIndex].AddInteractionPunch();

                if (!activated || this.isSolved)
                {
                    return false;
                }

                if (this.noPressed > 7)
                {
                    this.HandleStrike();
                }
                else
                {
                    this.answers[this.noPressed++] = myIndex;
                }
        
                return false;
            };
        }


        this.ActivateButton.OnInteract += delegate ()
        {
            Audio.PlaySoundAtTransform("Button sound", this.ActivateButton.transform);
            ActivateButton.AddInteractionPunch();
            if (this.isSolved)
            {
                return false;
            }

            if (this.activated)
            {
                this.HandleStrike();
            }
            else
            {
                Debug.LogFormat("[Burglar Alarm #{0}] Module activated!", this._moduleId);
                Debug.LogFormat("[Burglar Alarm #{0}] Expected input: {1}.", this._moduleId, string.Join(",", numberHandlers.Select(x => x.GetNumber().ToString()).ToArray()));
                StartCoroutine("Countdown");
                this.activationSound = Audio.PlaySoundAtTransformWithRef("Activation sound", Module.transform);
                this.activated = true;
            }

            return false;
        };

        this.SubmitButton.OnInteract += delegate ()
        {
            Audio.PlaySoundAtTransform("Button sound", this.SubmitButton.transform);
            SubmitButton.AddInteractionPunch();
            if (this.isSolved)
            {
                return false;
            }

            if (!this.activated || this.noPressed != 8)
            {
                this.HandleStrike();
                return false;
            }

            bool success = true;
            for (int i = 0; i < this.answers.Count(); ++i)
            {
                Debug.Log(string.Format("[Burglar Alarm #{0}] Answer {1} got {2}; expected {3}", this._moduleId, i, this.answers[i], this.numberHandlers[i].GetNumber()));
                if (this.answers[i] != this.numberHandlers[i].GetNumber())
                {
                    success = false;
                }
            }

            if (success)
            {
                Debug.LogFormat("[Burglar Alarm #{0}] Module passed!", this._moduleId);
                this.HandlePass();
            }
            else
            {
                Debug.LogFormat("[Burglar Alarm #{0}] Wrong answer!", this._moduleId);
                this.HandleStrike();
            }

            return false;
        };

    }

    private void HandleStrike()
    {
        StopCoroutine("Countdown");
        Audio.PlaySoundAtTransform("Strike sound effect", this.Module.transform);
        this.Module.HandleStrike();
        this.activated = false;
        this.isSolved = false;
        this.StopSound();
        this.noPressed = 0;
    }

    private void HandlePass()
    {
        StopCoroutine("Countdown");
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, this.Module.transform);
        this.Module.HandlePass();
        this.activated = false;
        this.isSolved = true;
        this.DisplayText.text = ("");
        this.StopSound();
        this.noPressed = 0;
    }

    private void StopSound()
    {
        if (this.activationSound != null)
        {
            this.activationSound.StopSound();
            this.activationSound = null;
        }
    }

    private IEnumerator Countdown()
    {
        for (int i = 1; i <= 15; i++)
        {
            yield return new WaitForSeconds(1f);
        }

        this.HandleTimeout();
        yield return null;
    }

    private void HandleTimeout()
    {
        if (this.activated == true)
        {
            Debug.LogFormat("[Burglar Alarm #{0}] Strike! Time ran out.", _moduleId);
            this.HandleStrike();
        }
    }



    //Twitch plays:
    KMSelectable[] ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();

        if (command.Equals("activate"))
        {
            return new[] { ActivateButton };
        }

        var match = TwitchPlayRegex.Match(command);
        if (match.Success)
        {
            return match.Groups[1].Value.Select(x => Buttons[int.Parse(x.ToString())]).Concat(new[] { SubmitButton }).ToArray();
        }

        return null;
    }

    public string TwitchHelpMessage = "Activate the module using !{0} activate , Submit the correct answer using ! {0} submit ######## .";

    // Update is called once per frame
    void Update () {
		
	}
}