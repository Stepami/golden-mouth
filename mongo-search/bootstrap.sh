#!/bin/sh
set -eu

umask 077
printf '%s' "$MONGO_SEARCH_PASSWORD" > /secrets/passwordFile

for attempt in $(seq 1 60); do
  if mongosh --quiet 'mongodb://localhost:27017/admin?directConnection=true' --eval 'quit(db.hello().isWritablePrimary ? 0 : 1)' >/dev/null 2>&1; then
    break
  fi
  sleep 1
done

mongosh --quiet 'mongodb://mongo:27017/admin?replicaSet=rs0' --eval '
  const admin = db.getSiblingDB("admin");
  if (admin.getUser("mongotUser")) {
    admin.updateUser("mongotUser", {pwd: process.env.MONGO_SEARCH_PASSWORD, roles: ["searchCoordinator"]});
  } else {
    admin.createUser({user: "mongotUser", pwd: process.env.MONGO_SEARCH_PASSWORD, roles: ["searchCoordinator"]});
  }
'
