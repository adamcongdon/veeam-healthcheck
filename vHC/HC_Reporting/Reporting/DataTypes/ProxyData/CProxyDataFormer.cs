﻿using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using VeeamHealthCheck.DataTypes;
using VeeamHealthCheck.Shared;

namespace VeeamHealthCheck.Reporting.DataTypes.ProxyData
{
    internal class CProxyDataFormer
    {
        public CProxyDataFormer() { }
        public void FormData()
        {

        }
        public string CalcProxyTasks(int assignedTasks, int cores, int ram)
        {
            int availableMem = ram - 2; //TODO double-check OS mem requirements
            int memTasks = (int)Math.Round((decimal)(availableMem / 2), 0, MidpointRounding.ToPositiveInfinity);
            int coreTasks = 0;
            
            if (cores == 0 && ram == 0)
                return "NA";

            if (CGlobals.vbrVersion == 11)
            {
                coreTasks = cores; //TODO need to imrprove this to cover 11a change
            }
            else if (CGlobals.vbrVersion == 12)
            {
                coreTasks = cores * 2;
            }

            return SetProvisionStatus(assignedTasks, cores, ram);


        }
        private string SetProvisionStatus(int assignedTasks, int coreTasks, int memTasks)
        {
            CProvisionTypes pt = new();



            if (coreTasks == memTasks)
            {
                if (assignedTasks == memTasks)
                    return pt.WellProvisioned;
                if (assignedTasks > memTasks)
                    return pt.OverProvisioned;
                if (assignedTasks < memTasks)
                    return pt.UnderProvisioned;
            }

            if (coreTasks < memTasks)
            {
                if (assignedTasks == coreTasks)
                    return pt.WellProvisioned;
                if (assignedTasks <= coreTasks)
                    return pt.UnderProvisioned;
                if (assignedTasks > coreTasks)
                    return pt.OverProvisioned;
            }
            if (coreTasks > memTasks)
            {
                if (assignedTasks == memTasks)
                    return pt.WellProvisioned;
                if (assignedTasks <= memTasks)
                    return pt.UnderProvisioned;
                if (assignedTasks > memTasks)
                    return pt.OverProvisioned;
            }



            return "NA";
        }
    }
}
