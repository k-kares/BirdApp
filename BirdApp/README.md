za pokretanje projekta u powershellu pokrenuti skriptu setup.ps1 (.\Scripts\setup.ps1)
skripta pokreće kafku, minio i mongodb u dockeru, te kreira potrebne baze i topice.
sama skripta također pokreće dotnet run pa pokrece projekt.
pri pokretanju skripte osigurajete da ste u dobrom folderu (.\BirdApp\BirdApp)
za brisanje cijelog docker okruženja možete pokrenuti skriptu clean.ps1 (.\Scripts\clean.ps1) 