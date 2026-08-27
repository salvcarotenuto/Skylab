-- Completa la mappa con due conti specifici SkyLab non presenti nel piano MicroFish di base.
INSERT INTO conti (Codice,Descrizione,Tipo,Mastro,Ditta,Locked,Carico) VALUES
  (108,'Acquisti di materie prime','C',21,'F',0,1),
  (109,'Ricavi delle prestazioni','R',31,'C',0,0);

UPDATE conti_legacy_map
SET ContoMicrofish=108, Regola='Acquisti di materie prime'
WHERE MastroLegacy=41 AND ContoLegacy=1;

UPDATE conti_legacy_map
SET ContoMicrofish=109, Regola='Ricavi delle prestazioni'
WHERE MastroLegacy=81 AND ContoLegacy=3;
