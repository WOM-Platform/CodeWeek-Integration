SHELL := /bin/bash

DC := docker-compose -f docker-compose.yml -f docker-compose.custom.yml
DC_RUN := ${DC} run --rm

include config.env
export

.PHONY: confirmation
confirmation:
	@echo -n 'Are you sure? [y|N] ' && read ans && [ $$ans == y ]

.PHONY: cmd
cmd:
	@echo 'Docker-Compose command:'
	@echo '${DC}'

.PHONY: install
install:
	${DC_RUN} database-client mysql -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE} < sql/database-create.sql

.PHONY: mysql
mysql: up
	${DC_RUN} database-client mysql -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE}

.PHONY: mysqlcmd
mysqlcmd:
	@echo 'MySQL client command:'
	@echo '${DC_RUN} database-client mysql -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE}'

.PHONY: dump
dump:
	${DC_RUN} database-client mysqldump -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE} > dump.sql
	@echo 'Database exported to dump.sql.'

.PHONY: import-db
import-db: confirmation
	test -f dump.sql
	@echo 'Replacing database with contents from file dump.sql...'
	${DC_RUN} database-client mysqldump -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE} > previous_dump.sql
	${DC_RUN} database-client mysql -h database -u ${MYSQL_USER} -p${MYSQL_PASSWORD} ${MYSQL_DATABASE} < dump.sql
	@echo 'Database replaced. Previous contents in previous_dump.sql.'

.PHONY: up
up:
	${DC} up -d web
	${DC} ps
	@echo
	@echo 'WOM Code Week service is now up'
	@echo

.PHONY: ps
ps:
	${DC} ps

.PHONY: rs
rs:
	${DC} restart

.PHONY: rebuild
rebuild:
	${DC} rm -sf web
	${DC} build web
	${DC} up -d web

.PHONY: stop
stop:
	${DC} stop

.PHONY: rm
rm:
	${DC} rm -fs

.PHONY: logs
logs:
	docker logs -f $(shell ${DC} ps -q web)
