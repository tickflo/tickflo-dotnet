-- Mark existing migrations as applied
INSERT INTO public.schema_migrations (version) 
VALUES ('20260113162007'), ('20260114145328')
ON CONFLICT (version) DO NOTHING;
