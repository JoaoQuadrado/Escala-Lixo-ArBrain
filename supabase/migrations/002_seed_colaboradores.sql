-- Seed inicial (opcional) — importa colaboradores existentes
insert into public.colaboradores (nome, usuario_discord, cargo, cor) values
  ('João Quadrado', '361476217209225218', 'Desenvolvedor', '#FFC300'),
  ('Raul', '1353892166279106601', 'Auxiliar', '#3B82F6'),
  ('Leticia Freitas', '1391757722218664042', 'Auxiliar', '#10B981'),
  ('Rafael Lopes', '1460348544983503243', 'Auxiliar', '#8B5CF6'),
  ('João Augusto', '1401956641577893918', 'Auxiliar', '#EC4899'),
  ('Bruna Palioto', '960673338848595970', 'Auxiliar', '#F97316'),
  ('Gustavo Carvalho', '1282721707169153086', 'Auxiliar', '#06B6D4'),
  ('Willian Fattori', '999691276494581820', 'Auxiliar', '#EF4444'),
  ('Ana', '1349345209460199446', 'Auxiliar', '#84CC16'),
  ('Giovana B', '264502649611616267', 'Auxiliar', '#6366F1'),
  ('Mariely Damaceno', '1337526203996831823', 'Auxiliar', '#FFC300'),
  ('Mateus', '1473677777193406497', 'Auxiliar', '#3B82F6'),
  ('Felipe', '1132038946461863936', 'Auxiliar', '#10B981'),
  ('Talia', '773997427442057216', 'Auxiliar', '#8B5CF6'),
  ('Nathan', '147514938997473291', 'Auxiliar', '#EC4899'),
  ('Dani', '1344080595327782985', 'Auxiliar', '#F97316'),
  ('Fernando', '1527427161085051010', 'Auxiliar', '#06B6D4'),
  ('Catarina', '803728474824900618', 'Auxiliar', '#EF4444')
on conflict (nome) do nothing;
