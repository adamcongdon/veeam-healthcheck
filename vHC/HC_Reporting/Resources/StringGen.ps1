﻿$content = Get-Content -LiteralPath "vhcres.txt"

foreach($line in $content){
    if(!$line.StartsWith("#")){

        $split = $line.Split()
        if($split -ne $null){
        
            if($split[0] -ne "#"){
                $string = "public static string " + $split[0] + " = m4.GetString(`"" + $split[0] + "`");"
                
                Write-Host($string)
                $string | Out-File -Append pubstrings.txt

            }
        }
    }
}