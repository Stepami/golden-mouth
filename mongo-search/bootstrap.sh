#!/bin/sh
set -eu

umask 077
printf '%s' "$MONGO_SEARCH_PASSWORD" > /secrets/passwordFile

mongosh --quiet 'mongodb://mongo:27017/admin?replicaSet=rs0' --eval '
  const admin = db.getSiblingDB("admin");
  if (admin.getUser("mongotUser")) {
    admin.updateUser("mongotUser", {pwd: process.env.MONGO_SEARCH_PASSWORD, roles: ["searchCoordinator"]});
  } else {
    admin.createUser({user: "mongotUser", pwd: process.env.MONGO_SEARCH_PASSWORD, roles: ["searchCoordinator"]});
  }
'
