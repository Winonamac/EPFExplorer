﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace EPFExplorer
{
    public partial class SpriteEditor : Form
    {
        public SpriteEditor()
        {
            InitializeComponent();
        }



        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            sprite.spriteEditor = null;

            base.OnClosing(e);
        }

        public int curFrame = 0;
        public archivedfile sprite;
        public List<rdtSubfileData> images = new List<rdtSubfileData>();
        public List<rdtSubfileData> palettes = new List<rdtSubfileData>();

        public List<Color> tempPalette;

        public bool ready = false;
        public void RequestSpriteEditorImage(int frame) {


            ready = false;

            for (int i = 0; i < images.Count; i++)  //load all other images if not already loaded
            {
                palettes[i].DecompressLZ10IfCompressed();
                if (images[i].image == null && i != frame)
                {
                    tempPalette = sprite.GetPalette(palettes[i].filebytes, 1, sprite.RDTSpriteBPP).ToList();

                    //for (int c = 0; c < tempPalette.Count; c++)
                      //  {
                        //tempPalette[c] = Color.FromArgb(0x00, tempPalette[c].R & 0xF8, tempPalette[c].G & 0xF8, tempPalette[c].B & 0xF8);
                        //}

                    sprite.RDTSpriteAlphaColour = tempPalette[0];
                    images[i].LoadImage(tempPalette.ToArray());
                    SetAlphaColourDisplay();
                }
            }

            if (images[frame].image == null)        //load selected image if not already loaded
            {
                palettes[frame].DecompressLZ10IfCompressed();
                tempPalette = sprite.GetPalette(palettes[frame].filebytes, 1, sprite.RDTSpriteBPP).ToList();
                
                //for (int c = 0; c < tempPalette.Count; c++)
                //{
                 //   tempPalette[c] = Color.FromArgb(0x00, tempPalette[c].R & 0xF8, tempPalette[c].G & 0xF8, tempPalette[c].B & 0xF8);
                //}

                sprite.RDTSpriteAlphaColour = tempPalette[0];
                images[frame].LoadImage(tempPalette.ToArray());
                SetAlphaColourDisplay();
            }

            if (sprite.RDTSpriteBPP == 8)
                {
                BPP_8_radioButton.Checked = true;    
                }
            else
                {
                BPP_4_radioButton.Checked = true;
                }

            offsetXUpDown.Value = images[frame].offsetX;
            offsetYUpDown.Value = images[frame].offsetY;

            ImageBox.Image = images[frame].image;
            curFrameDisplay.Text = "Frame " + (frame+1) + " / "+sprite.RDTSpriteNumFrames;
            durationBox.Value = sprite.RDTSpriteFrameDurations[curFrame];

            ready = true;
        }
      

  
        private void ImageBox_Click(object sender, EventArgs e)
        {

        }

        private void nextFrame_Click(object sender, EventArgs e)
        {
            if (loopingCheckbox.Checked && curFrame >= sprite.RDTSpriteNumFrames - 1) { curFrame = -1; }

            if (curFrame < sprite.RDTSpriteNumFrames - 1)
            {
                curFrame++;
                RequestSpriteEditorImage(curFrame);
            }
        }

        private void prevFrame_Click(object sender, EventArgs e)
        {
            if (loopingCheckbox.Checked && curFrame <= 0) { curFrame = sprite.RDTSpriteNumFrames; }

            if (curFrame > 0)
                {
                    curFrame--;
                    RequestSpriteEditorImage(curFrame); 
                }
        }

        private void deleteFrame_Click(object sender, EventArgs e)
        {
            if (images.Count < 2)
                {
                MessageBox.Show("You can't have zero frames!", "Cannot delete frame", MessageBoxButtons.OK);
                return;
                }

            images.RemoveAt(curFrame);
            palettes.RemoveAt(curFrame);
            sprite.RDTSpriteFrameDurations.RemoveAt(curFrame);

            if (curFrame >= images.Count)
                {
                curFrame = images.Count - 1;
                }

            sprite.RDTSpriteNumFrames--;

            SendUpdateToRDT();
        }


        public void SendUpdateToRDT() {

            ready = false;

            //remove the existing images and palettes in the archivedfile
            int indexOfFirstImageOrPalette = 0;

            foreach (rdtSubfileData f in sprite.rdtSubfileDataList)
            {
                if (f.graphicsType != "image" && f.graphicsType != "palette")
                {
                    indexOfFirstImageOrPalette++;
                    continue;
                }
                break;
            }

            sprite.rdtSubfileDataList.RemoveRange(indexOfFirstImageOrPalette, sprite.rdtSubfileDataList.Count - indexOfFirstImageOrPalette);


            //readd the edited ones

            for (int i = 0; i < images.Count; i++)
            {
                sprite.rdtSubfileDataList.Add(palettes[i]);
                sprite.rdtSubfileDataList.Add(images[i]);
            }

            RequestSpriteEditorImage(curFrame);

            ready = true;
        }

        private void durationBox_ValueChanged(object sender, EventArgs e)
        {
            sprite.RDTSpriteFrameDurations[curFrame] = (ushort)durationBox.Value;
            SendUpdateToRDT();
        }

        private void replaceFrame_Click(object sender, EventArgs e)
        {
            GetNewFrameImageFromFile(false);
        }


        public void GetNewFrameImageFromFile(bool addingFrame) {

            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Title = "Select image file";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;
            openFileDialog1.Filter = "png (*.png)|*.png";

            bool failed = false;

            rdtSubfileData oldImage = new rdtSubfileData(images[curFrame]);
            List<Color> oldPalette = new List<Color>(tempPalette);

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                images[curFrame].image = Image.FromFile(openFileDialog1.FileName);

                //make palette
                tempPalette = new List<Color>();

                Color potentialNewColour;

                for (int y = 0; y < images[curFrame].image.Height; y++)
                    {
                    for (int x = 0; x < images[curFrame].image.Width; x++)
                        {
                        potentialNewColour = ((Bitmap)images[curFrame].image).GetPixel(x,y);
                        potentialNewColour = Color.FromArgb(0x00, potentialNewColour.R & 0xF8, potentialNewColour.G & 0xF8, potentialNewColour.B & 0xF8);

                        if (!tempPalette.Contains(potentialNewColour))
                            {
                            tempPalette.Add(potentialNewColour);
                            }
                        
                        if (tempPalette.Count > 256)
                            {
                            break;
                            }
                        }
                    }

                if (sprite.RDTSpriteBPP == 4 && tempPalette.Count > 16)
                    {
                    MessageBox.Show("For a 4BPP sprite, you should have a maximum of 16 colours.\nYou had: "+tempPalette.Count + " colours.", "Too many colours!", MessageBoxButtons.OK);
                    failed = true;
                    }
                else if (sprite.RDTSpriteBPP == 8 && tempPalette.Count > 256)
                    {
                    MessageBox.Show("For an 8BPP sprite, you should have a maximum of 256 colours.\nYou had: " + tempPalette.Count+" colours.", "Too many colours!", MessageBoxButtons.OK);
                    failed = true;
                    }

                if (!failed)
                    {
                    //guess alpha colour
                    sprite.RDTSpriteAlphaColour = ((Bitmap)images[curFrame].image).GetPixel(0, 0);   //assume the top left corner is the alpha colour. The user can change this later if they wish
                    sprite.RDTSpriteAlphaColour = Color.FromArgb(0x00, sprite.RDTSpriteAlphaColour.R & 0xF8, sprite.RDTSpriteAlphaColour.G & 0xF8, sprite.RDTSpriteAlphaColour.B & 0xF8);
                    SetAlphaColourDisplay();
                    }
                }
            else
                {
                failed = true;
                }

            if (failed)
                {
                if (addingFrame)    //undo the addition of the new frame
                    {
                    images.RemoveAt(curFrame);
                    palettes.RemoveAt(curFrame);
                    sprite.RDTSpriteFrameDurations.RemoveAt(curFrame);
                    sprite.RDTSpriteNumFrames--;
                    curFrame--;
                    }
                else                //revert to the old frame
                    {
                    images[curFrame] = oldImage;
                    tempPalette = oldPalette;
                    }
                }


            SendUpdateToRDT();
        }

        private void addFrame_Click(object sender, EventArgs e)
        {
            sprite.RDTSpriteNumFrames++;
            curFrame++;

            rdtSubfileData newSubFileData = new rdtSubfileData(images[curFrame - 1]);

            images.Insert(curFrame,newSubFileData);   //create new dummy image for this to fill
            palettes.Insert(curFrame, palettes[curFrame - 1]); //create new dummy image for this to fill
            sprite.RDTSpriteFrameDurations.Insert(curFrame, sprite.RDTSpriteFrameDurations[curFrame - 1]);

            GetNewFrameImageFromFile(true);
        }

        private void exportFrame_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Title = "Save frame";
            saveFileDialog1.FileName = Path.GetFileName(sprite.filename+"_"+(curFrame+1));
            saveFileDialog1.CheckPathExists = true;
            saveFileDialog1.DefaultExt = ".png";
            saveFileDialog1.Filter = "png (*.png)|*.png";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                images[curFrame].image.Save(saveFileDialog1.FileName);
            }
        }

        private void alphaColourPrev_Click(object sender, EventArgs e)
        {
            if (tempPalette.IndexOf(sprite.RDTSpriteAlphaColour) > 0)
                {
                sprite.RDTSpriteAlphaColour = tempPalette[tempPalette.IndexOf(sprite.RDTSpriteAlphaColour) - 1];
                }
            else
                {
                sprite.RDTSpriteAlphaColour = tempPalette[tempPalette.Count - 1];
                }

            SetAlphaColourDisplay();
        }

        private void alphaColourNext_Click(object sender, EventArgs e)
        {
            if (tempPalette.IndexOf(sprite.RDTSpriteAlphaColour) < tempPalette.Count - 1)
            {
                sprite.RDTSpriteAlphaColour = tempPalette[tempPalette.IndexOf(sprite.RDTSpriteAlphaColour) + 1];
            }
            else
            {
                sprite.RDTSpriteAlphaColour = tempPalette[0];
            }

            SetAlphaColourDisplay();
        }

        private void alphacolourdisplay_Click(object sender, EventArgs e)
        {

        }

        public void SetAlphaColourDisplay() {

            alphacolourdisplay.BackColor = tempPalette[tempPalette.IndexOf(sprite.RDTSpriteAlphaColour)];
        
        }

        private void exportRawFilesButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog openFileDialog1 = new SaveFileDialog();

            openFileDialog1.Title = "Save raw files";
            openFileDialog1.FileName = Path.GetFileName(sprite.filename);
            openFileDialog1.CheckPathExists = true;


            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                foreach (rdtSubfileData f in sprite.rdtSubfileDataList)
                    {
                    File.WriteAllBytes(openFileDialog1.FileName + "_" + sprite.rdtSubfileDataList.IndexOf(f), f.filebytes);
                    }
                }
        }

        private void BPP_4_radioButton_CheckedChanged(object sender, EventArgs e)
        {
            sprite.RDTSpriteBPP = 4;
        }

        private void BPP_8_radioButton_CheckedChanged(object sender, EventArgs e)
        {
            sprite.RDTSpriteBPP = 8;
        }

        private void offsetXUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (!ready) { return; }
            images[curFrame].offsetX = (ushort)offsetXUpDown.Value;
            SendUpdateToRDT();
        }

        private void offsetYUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (!ready) { return; }
            images[curFrame].offsetY = (ushort)offsetYUpDown.Value;
            SendUpdateToRDT();
        }

        private void centreX_ValueChanged(object sender, EventArgs e)
        {
            if (!ready) { return; }
            ChangeSpriteSetting("center",(int)centreX.Value, (int)centreY.Value, 0, 0);
            SendUpdateToRDT();
        }


        public void ChangeSpriteSetting(string settingName, int value1, int value2, int value3, int value4) {

            List<rdtSubfileData.setting> spriteSettings = sprite.rdtSubfileDataList[1].spriteSettings;

            if (spriteSettings == null || !ready)
                {
                return;
                }

            foreach (rdtSubfileData.setting s in spriteSettings)
                {
                if (s.name == settingName)
                    {
                    switch (s.type)
                        {
                        case 0x03: //bool
                            if (value1 == 1)
                                {
                                s.trueOrFalse = true;
                                }
                            else
                                {
                                s.trueOrFalse = false;
                                }
                            break;
                        case 0x04: //vector2
                            s.X = value1;
                            s.Y = value2;
                            break;

                        case 0x05: //2d rect
                            s.X = value1;
                            s.Y = value2;
                            s.X2 = value3;
                            s.Y2 = value4;
                            break;
                        }
                    return;
                    }
                }

            //if it gets to this point, then the setting wasn't found, so create it, then run the function again.

            rdtSubfileData.setting newSetting = new rdtSubfileData.setting();
            newSetting.name = settingName;

            switch (settingName)
                {
                case "looping":
                case "rotatable":
                case "isOAMSprite":
                    newSetting.type = 0x03;
                    break;
                case "center":
                    newSetting.type = 0x04;
                    break;
                case "bounds":
                    newSetting.type = 0x05;
                    break;
                }

            spriteSettings.Add(newSetting);

            ChangeSpriteSetting(settingName, value1, value2, value3, value4);
            }

        private void centreY_ValueChanged(object sender, EventArgs e)
        {
            if (!ready) { return; }
            ChangeSpriteSetting( "center", (int)centreX.Value, (int)centreY.Value, 0, 0);
            SendUpdateToRDT();
        }

        private void boundsX_ValueChanged(object sender, EventArgs e)
        {
            if (!ready) { return; }
            ChangeSpriteSetting( "bounds", (int)boundsX.Value, (int)boundsY.Value, (int)boundsX2.Value, (int)boundsY2.Value);
            SendUpdateToRDT();
        }

        private void boundsX2_ValueChanged(object sender, EventArgs e)
        {
            if (!ready) { return; }
            ChangeSpriteSetting( "bounds", (int)boundsX.Value, (int)boundsY.Value, (int)boundsX2.Value, (int)boundsY2.Value);
            SendUpdateToRDT();
        }

        private void boundsY_ValueChanged(object sender, EventArgs e)
        {
            if (!ready) { return; }
            ChangeSpriteSetting( "bounds", (int)boundsX.Value, (int)boundsY.Value, (int)boundsX2.Value, (int)boundsY2.Value);
            SendUpdateToRDT();
        }

        private void boundsY2_ValueChanged(object sender, EventArgs e)
        {
            if (!ready) { return; }
            ChangeSpriteSetting( "bounds", (int)boundsX.Value, (int)boundsY.Value, (int)boundsX2.Value, (int)boundsY2.Value);
            SendUpdateToRDT();
        }

        private void OAMSpriteCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (!ready) { return; }

            if (OAMSpriteCheckbox.Checked)
            {
                ChangeSpriteSetting( "isOAMSprite", 1, 0, 0, 0);
            }
            else
            {
                ChangeSpriteSetting( "isOAMSprite", 0, 0, 0, 0);
            }
            SendUpdateToRDT();
        }

        private void loopingCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (!ready) { return; }

            if (loopingCheckbox.Checked)
            {
                ChangeSpriteSetting( "looping", 1, 0, 0, 0);
            }
            else
            {
                ChangeSpriteSetting( "looping", 0, 0, 0, 0);
            }
            SendUpdateToRDT();
        }

        private void rotatableCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if(!ready){return;}

            if (rotatableCheckbox.Checked)
            {
                ChangeSpriteSetting( "rotatable", 1, 0, 0, 0);
            }
            else
            {
                ChangeSpriteSetting( "rotatable", 0, 0, 0, 0);
            }
            SendUpdateToRDT();
        }
    }
}
